using Booking.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Booking.API.Services;

/// <summary>
/// Default implementation of IRealtimeNotificationService.
/// Integrates with SignalR BookingHub for real-time notifications.
/// </summary>
public sealed class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<Booking.API.Hubs.BookingHub> _hubContext;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<Booking.API.Hubs.BookingHub> hubContext,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifySeatStatusChangedAsync(Guid eventId, Guid seatId, string newStatus, string previousStatus)
    {
        try
        {
            await _hubContext.Clients
                .Group($"event_{eventId}")
                .SendAsync("SeatStatusChanged", new
                {
                    eventId,
                    seatId,
                    newStatus,
                    previousStatus,
                    changedAt = DateTime.UtcNow
                });

            _logger.LogInformation(
                "Seat status change notification sent for Seat: {SeatId}, Event: {EventId}",
                seatId,
                eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending seat status change notification");
        }
    }

    public async Task NotifyReservationCreatedAsync(Guid eventId, Guid seatId, string userEmail)
    {
        try
        {
            await _hubContext.Clients
                .Group($"event_{eventId}")
                .SendAsync("ReservationCreated", new
                {
                    eventId,
                    seatId,
                    userEmail,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation(
                "Reservation created notification sent for Event: {EventId}, User: {UserEmail}",
                eventId,
                userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reservation created notification");
        }
    }

    public async Task NotifyPaymentConfirmedAsync(Guid eventId, Guid seatId, string userEmail)
    {
        try
        {
            await _hubContext.Clients
                .Group($"event_{eventId}")
                .SendAsync("PaymentConfirmed", new
                {
                    eventId,
                    seatId,
                    userEmail,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation(
                "Payment confirmed notification sent for Event: {EventId}, User: {UserEmail}",
                eventId,
                userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment confirmed notification");
        }
    }
}
