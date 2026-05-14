namespace Booking.Infrastructure.Services;

/// <summary>
/// Background service that periodically cleans up expired seat holds.
/// Runs every 5 minutes to check for reservations with expired hold timers.
/// Releases associated seats back to "Available" status and cancels the reservation.
/// 
/// Key Responsibilities:
/// - Prevents indefinite seat locking without payment
/// - Maintains temporal inventory guarantees
/// - Provides audit trail of expired holds
/// 
/// Concurrency: Uses pessimistic locking when updating seats and reservations
/// to prevent race conditions with active booking operations.
/// </summary>
public sealed class HoldCleanupService : BackgroundService
{
    private readonly ILogger<HoldCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Check every minute for more responsiveness
    private const int BatchSize = 100; // Process expired holds in batches

    public HoldCleanupService(
        ILogger<HoldCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hold Cleanup Service is starting");

        // Initial delay to allow services to initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredHoldsAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Hold Cleanup Service is stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in Hold Cleanup Service");
                // Continue running even on error, retry after delay
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Scans for expired holds and releases them back to available status.
    /// Processes in batches to avoid overwhelming the database.
    /// </summary>
    private async Task CleanupExpiredHoldsAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            try
            {
                var expiredReservations = await reservationRepository.GetExpiredHoldsAsync(cancellationToken);

                if (expiredReservations.Count == 0)
                {
                    _logger.LogDebug("No expired holds found during cleanup cycle");
                    return;
                }

                _logger.LogInformation(
                    "Starting cleanup of {Count} expired holds",
                    expiredReservations.Count);

                int successCount = 0;
                int failureCount = 0;

                // Process in batches
                var batches = expiredReservations
                    .Chunk(BatchSize)
                    .ToList();

                foreach (var batch in batches)
                {
                    foreach (var reservation in batch)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Hold Cleanup Service received cancellation request");
                            return;
                        }

                        try
                        {
                            await ProcessExpiredHoldAsync(
                                reservation,
                                reservationRepository,
                                eventRepository,
                                cancellationToken);

                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failureCount++;
                            _logger.LogError(
                                ex,
                                "Error processing expired hold for reservation {ReservationId}",
                                reservation.Id);
                        }
                    }
                }

                _logger.LogInformation(
                    "Hold cleanup cycle completed. Processed: {Total}, Succeeded: {Success}, Failed: {Failure}",
                    expiredReservations.Count,
                    successCount,
                    failureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during hold cleanup cycle");
            }
        }
    }

    /// <summary>
    /// Processes a single expired reservation hold.
    /// 1. Fetches the event with pessimistic lock
    /// 2. Releases the seat back to Available
    /// 3. Cancels the reservation
    /// 4. Persists changes
    /// </summary>
    private async Task ProcessExpiredHoldAsync(
        Reservation reservation,
        IReservationRepository reservationRepository,
        IEventRepository eventRepository,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Processing expired hold. ReservationId: {ReservationId}, EventId: {EventId}, SeatId: {SeatId}, ExpiresAt: {ExpiresAt}",
            reservation.Id,
            reservation.EventId,
            reservation.SeatId,
            reservation.HoldExpiresAtUtc);

        // Fetch event with pessimistic lock to prevent conflicts with active bookings
        var @event = await eventRepository.GetByIdWithLockAsync(reservation.EventId, cancellationToken);
        if (@event is null)
        {
            _logger.LogWarning(
                "Event {EventId} not found for expired hold cleanup. ReservationId: {ReservationId}",
                reservation.EventId,
                reservation.Id);
            return;
        }

        var seat = @event.GetSeat(reservation.SeatId);
        if (seat is null)
        {
            _logger.LogWarning(
                "Seat {SeatId} not found in event {EventId} for expired hold cleanup. ReservationId: {ReservationId}",
                reservation.SeatId,
                reservation.EventId,
                reservation.Id);
            return;
        }

        // Only release if seat is still held (status may have changed)
        if (seat.Status.Value == "Held")
        {
            try
            {
                seat.Release();
                _logger.LogDebug(
                    "Seat released from hold. ReservationId: {ReservationId}, SeatId: {SeatId}",
                    reservation.Id,
                    seat.Id);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to release seat during expired hold cleanup. ReservationId: {ReservationId}, SeatId: {SeatId}",
                    reservation.Id,
                    seat.Id);
                return;
            }
        }
        else
        {
            _logger.LogDebug(
                "Seat is no longer in Held status, skipping release. ReservationId: {ReservationId}, SeatId: {SeatId}, CurrentStatus: {Status}",
                reservation.Id,
                seat.Id,
                seat.Status.Value);
        }

        // Cancel the reservation if not already cancelled
        if (reservation.Status.Value != "Cancelled")
        {
            try
            {
                reservation.Cancel();
                _logger.LogDebug(
                    "Reservation cancelled due to expired hold. ReservationId: {ReservationId}",
                    reservation.Id);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to cancel reservation during expired hold cleanup. ReservationId: {ReservationId}",
                    reservation.Id);
                return;
            }
        }

        // Persist changes
        await eventRepository.UpdateAsync(@event, cancellationToken);
        await reservationRepository.UpdateAsync(reservation, cancellationToken);

        _logger.LogInformation(
            "Successfully released expired hold. ReservationId: {ReservationId}, EventId: {EventId}, SeatId: {SeatId}",
            reservation.Id,
            reservation.EventId,
            reservation.SeatId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hold Cleanup Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
