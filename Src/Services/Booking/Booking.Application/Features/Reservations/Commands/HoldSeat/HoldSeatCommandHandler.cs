using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.Reservations.Commands.HoldSeat;

/// <summary>
/// Handles seat hold requests with concurrent transaction support.
/// Uses pessimistic locking to prevent double-booking in high-traffic scenarios.
/// Implements optimistic retry logic for handling concurrent modifications.
/// </summary>
public sealed class HoldSeatCommandHandler : IRequestHandler<HoldSeatCommand, Response<ReservationDto>>
{
    private const int HoldDurationMinutes = 10;
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 50;

    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationMapper _mapper;
    private readonly ILogger<HoldSeatCommandHandler> _logger;

    public HoldSeatCommandHandler(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        IReservationMapper mapper,
        ILogger<HoldSeatCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async ValueTask<Response<ReservationDto>> Handle(
        HoldSeatCommand request,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                return await ProcessHoldWithLockingAsync(request, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrency conflict on attempt {Attempt}/{MaxAttempts} for seat hold. UserId: {UserId}, EventId: {EventId}, SeatId: {SeatId}",
                    attempt + 1,
                    MaxRetryAttempts,
                    request.UserId,
                    request.EventId,
                    request.SeatId);

                if (attempt == MaxRetryAttempts - 1)
                {
                    _logger.LogError(
                        "Max retry attempts exceeded for seat hold. UserId: {UserId}, EventId: {EventId}, SeatId: {SeatId}",
                        request.UserId,
                        request.EventId,
                        request.SeatId);

                    return Response<ReservationDto>.Failure(
                        new Error("Seat.HoldFailed", "Seat became unavailable. Please try again."));
                }

                // Exponential backoff retry
                await Task.Delay(RetryDelayMs * (int)Math.Pow(2, attempt), cancellationToken);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Domain validation error during seat hold. UserId: {UserId}, EventId: {EventId}, SeatId: {SeatId}",
                    request.UserId,
                    request.EventId,
                    request.SeatId);

                return Response<ReservationDto>.Failure(
                    new Error("Seat.HoldFailed", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during seat hold. UserId: {UserId}, EventId: {EventId}, SeatId: {SeatId}",
                    request.UserId,
                    request.EventId,
                    request.SeatId);

                return Response<ReservationDto>.Failure(
                    new Error("Seat.HoldFailed", "An unexpected error occurred while holding the seat"));
            }
        }

        return Response<ReservationDto>.Failure(
            new Error("Seat.HoldFailed", "Unable to hold seat after multiple attempts"));
    }

    private async ValueTask<Response<ReservationDto>> ProcessHoldWithLockingAsync(
        HoldSeatCommand request,
        CancellationToken cancellationToken)
    {
        // Use pessimistic locking to prevent concurrent seat modifications
        var @event = await _eventRepository.GetByIdWithLockAsync(request.EventId, cancellationToken);
        if (@event is null)
        {
            _logger.LogWarning("Event not found. EventId: {EventId}", request.EventId);
            return Response<ReservationDto>.Failure(
                new Error("Event.NotFound", "Event not found"));
        }

        var seat = @event.GetSeat(request.SeatId);
        if (seat is null)
        {
            _logger.LogWarning("Seat not found. EventId: {EventId}, SeatId: {SeatId}", request.EventId, request.SeatId);
            return Response<ReservationDto>.Failure(
                new Error("Seat.NotFound", "Seat not found"));
        }

        // Validate seat status - must be Available
        if (seat.Status != SeatStatus.Available)
        {
            _logger.LogInformation(
                "Seat unavailable for hold. EventId: {EventId}, SeatId: {SeatId}, Status: {Status}",
                request.EventId,
                request.SeatId,
                seat.Status.Value);

            return Response<ReservationDto>.Failure(
                new Error("Seat.Unavailable", $"Seat is {seat.Status.Value}"));
        }

        // Check for existing pending reservation using pessimistic lock
        var existingReservation = await _reservationRepository
            .GetByEventAndSeatWithLockAsync(request.EventId, request.SeatId, cancellationToken);

        if (existingReservation is not null && existingReservation.Status == ReservationStatus.Pending)
        {
            _logger.LogInformation(
                "Pending reservation already exists for seat. ReservationId: {ReservationId}, EventId: {EventId}, SeatId: {SeatId}",
                existingReservation.Id,
                request.EventId,
                request.SeatId);

            return Response<ReservationDto>.Failure(
                new Error("Reservation.AlreadyPending", "Seat is already on hold"));
        }

        // Hold the seat with 10-minute expiration
        seat.Hold(TimeSpan.FromMinutes(HoldDurationMinutes));

        // Create reservation with matching hold duration
        var reservation = Reservation.Create(
            request.UserId,
            request.EventId,
            request.SeatId,
            TimeSpan.FromMinutes(HoldDurationMinutes));

        // Persist changes
        await _eventRepository.UpdateAsync(@event, cancellationToken);
        await _reservationRepository.AddAsync(reservation, cancellationToken);

        _logger.LogInformation(
            "Seat successfully held. ReservationId: {ReservationId}, UserId: {UserId}, EventId: {EventId}, SeatId: {SeatId}, ExpiresAt: {ExpiresAt}",
            reservation.Id,
            request.UserId,
            request.EventId,
            request.SeatId,
            reservation.HoldExpiresAtUtc);

        return Response<ReservationDto>.Success(_mapper.MapToDto(reservation));
    }
}
