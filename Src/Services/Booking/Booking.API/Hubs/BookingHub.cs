using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Booking.Application.Common.Interfaces;
using System.Security.Claims;

namespace Booking.API.Hubs;

/// <summary>
/// SignalR hub for real-time seat status updates.
/// Provides live notifications when seat status changes (Available -> Held -> Reserved -> Sold).
/// Secured with JWT authentication via JwtBearerDefaults.
/// </summary>
[Authorize]
public sealed class BookingHub : Hub
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingHub> _logger;

    // Group names for organizing connections
    private const string SeatGroupPrefix = "seat_";
    private const string EventGroupPrefix = "event_";

    public BookingHub(
        IEventRepository eventRepository,
        ILogger<BookingHub> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} connected to BookingHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} disconnected from BookingHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes a client to real-time seat status updates for a specific event.
    /// The client will receive notifications whenever a seat status changes in this event.
    /// </summary>
    /// <param name="eventId">The event ID to subscribe to</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SubscribeToEventSeats(Guid eventId)
    {
        try
        {
            var @event = await _eventRepository.GetByIdAsync(eventId);
            if (@event is null)
            {
                _logger.LogWarning("Subscription failed: Event {EventId} not found", eventId);
                await Clients.Caller.SendAsync("SubscriptionFailed", new { message = "Event not found" });
                return;
            }

            var groupName = $"{EventGroupPrefix}{eventId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(
                "Connection {ConnectionId} subscribed to event {EventId} seat updates",
                Context.ConnectionId,
                eventId);

            await Clients.Caller.SendAsync("SubscriptionSucceeded", new { eventId, groupName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to event {EventId}", eventId);
            await Clients.Caller.SendAsync("SubscriptionFailed", new { message = "An error occurred while subscribing" });
        }
    }

    /// <summary>
    /// Unsubscribes a client from real-time seat status updates for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID to unsubscribe from</param>
    /// <returns>Task representing the async operation</returns>
    public async Task UnsubscribeFromEventSeats(Guid eventId)
    {
        try
        {
            var groupName = $"{EventGroupPrefix}{eventId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(
                "Connection {ConnectionId} unsubscribed from event {EventId} seat updates",
                Context.ConnectionId,
                eventId);

            await Clients.Caller.SendAsync("UnsubscriptionSucceeded", new { eventId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from event {EventId}", eventId);
        }
    }

    /// <summary>
    /// Subscribes a client to updates for a specific seat.
    /// Useful for real-time status updates on a seat being viewed.
    /// </summary>
    /// <param name="seatId">The seat ID to subscribe to</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SubscribeToSeat(Guid seatId)
    {
        try
        {
            var groupName = $"{SeatGroupPrefix}{seatId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(
                "Connection {ConnectionId} subscribed to seat {SeatId} updates",
                Context.ConnectionId,
                seatId);

            await Clients.Caller.SendAsync("SeatSubscriptionSucceeded", new { seatId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to seat {SeatId}", seatId);
        }
    }

    /// <summary>
    /// Unsubscribes a client from updates for a specific seat.
    /// </summary>
    /// <param name="seatId">The seat ID to unsubscribe from</param>
    /// <returns>Task representing the async operation</returns>
    public async Task UnsubscribeFromSeat(Guid seatId)
    {
        try
        {
            var groupName = $"{SeatGroupPrefix}{seatId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(
                "Connection {ConnectionId} unsubscribed from seat {SeatId} updates",
                Context.ConnectionId,
                seatId);

            await Clients.Caller.SendAsync("SeatUnsubscriptionSucceeded", new { seatId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from seat {SeatId}", seatId);
        }
    }

    /// <summary>
    /// Server method: Notifies all connected clients in an event group about a seat status change.
    /// Called by application handlers when a seat status is updated.
    /// </summary>
    /// <param name="eventId">The event ID</param>
    /// <param name="seatId">The seat ID</param>
    /// <param name="newStatus">The new seat status</param>
    /// <param name="previousStatus">The previous seat status</param>
    public async Task NotifySeatStatusChanged(Guid eventId, Guid seatId, string newStatus, string previousStatus)
    {
        var groupName = $"{EventGroupPrefix}{eventId}";
        var seatGroupName = $"{SeatGroupPrefix}{seatId}";

        var notification = new
        {
            eventId,
            seatId,
            newStatus,
            previousStatus,
            changedAt = DateTime.UtcNow
        };

        await Clients.Group(groupName).SendAsync("SeatStatusChanged", notification);
        await Clients.Group(seatGroupName).SendAsync("SeatStatusChanged", notification);

        _logger.LogInformation(
            "Seat status change notification sent. Seat: {SeatId}, Event: {EventId}, Status: {PreviousStatus} -> {NewStatus}",
            seatId,
            eventId,
            previousStatus,
            newStatus);
    }

    /// <summary>
    /// Server method: Notifies clients about a successful reservation.
    /// </summary>
    public async Task NotifyReservationCreated(Guid eventId, Guid seatId, string userEmail)
    {
        var groupName = $"{EventGroupPrefix}{eventId}";
        var notification = new
        {
            eventId,
            seatId,
            userEmail,
            eventType = "ReservationCreated",
            timestamp = DateTime.UtcNow
        };

        await Clients.Group(groupName).SendAsync("ReservationCreated", notification);
    }

    /// <summary>
    /// Server method: Notifies clients about a successful payment confirmation.
    /// </summary>
    public async Task NotifyPaymentConfirmed(Guid eventId, Guid seatId, string userEmail)
    {
        var groupName = $"{EventGroupPrefix}{eventId}";
        var notification = new
        {
            eventId,
            seatId,
            userEmail,
            eventType = "PaymentConfirmed",
            timestamp = DateTime.UtcNow
        };

        await Clients.Group(groupName).SendAsync("PaymentConfirmed", notification);
    }
}
