namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Service for coordinating real-time SignalR notifications from application handlers.
/// This service bridges the application layer with the SignalR hub.
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>
    /// Sends a seat status change notification to all connected clients.
    /// </summary>
    Task NotifySeatStatusChangedAsync(Guid eventId, Guid seatId, string newStatus, string previousStatus);

    /// <summary>
    /// Sends a reservation created notification to event subscribers.
    /// </summary>
    Task NotifyReservationCreatedAsync(Guid eventId, Guid seatId, string userEmail);

    /// <summary>
    /// Sends a payment confirmed notification to event subscribers.
    /// </summary>
    Task NotifyPaymentConfirmedAsync(Guid eventId, Guid seatId, string userEmail);
}
