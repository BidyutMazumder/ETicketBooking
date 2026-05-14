using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.Reservations.Commands.ConfirmBooking;

/// <summary>
/// Handles booking confirmation after payment has been processed.
/// Transitions seat from Held -> Reserved -> Sold during the booking lifecycle.
/// Uses pessimistic locking to prevent race conditions during seat status transitions.
/// </summary>
public sealed class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, Response<ReservationDto>>
{
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 50;

    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationMapper _mapper;
    private readonly ILogger<ConfirmBookingCommandHandler> _logger;

    public ConfirmBookingCommandHandler(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        IReservationMapper mapper,
        ILogger<ConfirmBookingCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async ValueTask<Response<ReservationDto>> Handle(
        ConfirmBookingCommand request,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                return await ProcessBookingConfirmationWithLockingAsync(request, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrency conflict on attempt {Attempt}/{MaxAttempts} for booking confirmation. ReservationId: {ReservationId}",
                    attempt + 1,
                    MaxRetryAttempts,
                    request.ReservationId);

                if (attempt == MaxRetryAttempts - 1)
                {
                    _logger.LogError(
                        "Max retry attempts exceeded for booking confirmation. ReservationId: {ReservationId}",
                        request.ReservationId);

                    return Response<ReservationDto>.Failure(
                        new Error("Reservation.ConfirmFailed", "Booking confirmation encountered a conflict. Please try again."));
                }

                await Task.Delay(RetryDelayMs * (int)Math.Pow(2, attempt), cancellationToken);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Domain validation error during booking confirmation. ReservationId: {ReservationId}",
                    request.ReservationId);

                return Response<ReservationDto>.Failure(
                    new Error("Reservation.ConfirmFailed", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error confirming booking. ReservationId: {ReservationId}",
                    request.ReservationId);

                return Response<ReservationDto>.Failure(
                    new Error("Reservation.ConfirmFailed", "An unexpected error occurred while confirming booking"));
            }
        }

        return Response<ReservationDto>.Failure(
            new Error("Reservation.ConfirmFailed", "Booking confirmation failed after multiple attempts"));
    }

    private async ValueTask<Response<ReservationDto>> ProcessBookingConfirmationWithLockingAsync(
        ConfirmBookingCommand request,
        CancellationToken cancellationToken)
    {
        // Fetch reservation with pessimistic lock
        var reservation = await _reservationRepository.GetByIdWithLockAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", request.ReservationId);
            return Response<ReservationDto>.Failure(
                new Error("Reservation.NotFound", "Reservation not found"));
        }

        // Verify payment has been confirmed
        if (reservation.PaymentStatus != PaymentStatus.Paid)
        {
            _logger.LogWarning(
                "Attempt to confirm booking without payment. ReservationId: {ReservationId}, PaymentStatus: {PaymentStatus}",
                request.ReservationId,
                reservation.PaymentStatus.Value);

            return Response<ReservationDto>.Failure(
                new Error("Payment.NotConfirmed", "Payment must be confirmed before booking"));
        }

        // Verify hold hasn't expired
        if (reservation.IsHoldExpired())
        {
            _logger.LogWarning(
                "Attempt to confirm expired hold. ReservationId: {ReservationId}, ExpiresAt: {ExpiresAt}",
                request.ReservationId,
                reservation.HoldExpiresAtUtc);

            return Response<ReservationDto>.Failure(
                new Error("Reservation.HoldExpired", "Hold has expired"));
        }

        // Fetch event with pessimistic lock
        var @event = await _eventRepository.GetByIdWithLockAsync(reservation.EventId, cancellationToken);
        if (@event is null)
        {
            _logger.LogWarning("Event {EventId} not found for reservation {ReservationId}", reservation.EventId, request.ReservationId);
            return Response<ReservationDto>.Failure(
                new Error("Event.NotFound", "Event not found"));
        }

        // Get the seat
        var seat = @event.GetSeat(reservation.SeatId);
        if (seat is null)
        {
            _logger.LogWarning(
                "Seat {SeatId} not found in event {EventId} for reservation {ReservationId}",
                reservation.SeatId,
                reservation.EventId,
                request.ReservationId);

            return Response<ReservationDto>.Failure(
                new Error("Seat.NotFound", "Seat not found"));
        }

        // Validate seat is in Held state (not yet reserved)
        if (seat.Status != SeatStatus.Held)
        {
            _logger.LogWarning(
                "Seat in unexpected status for booking confirmation. ReservationId: {ReservationId}, SeatId: {SeatId}, Status: {Status}",
                request.ReservationId,
                reservation.SeatId,
                seat.Status.Value);

            return Response<ReservationDto>.Failure(
                new Error("Seat.InvalidStatus", $"Seat must be held to confirm booking, but is {seat.Status.Value}"));
        }

        // Transition seat: Held -> Reserved
        seat.Reserve();

        // Transition reservation: Pending -> Confirmed
        reservation.Confirm();

        // Persist changes
        await _eventRepository.UpdateAsync(@event, cancellationToken);
        await _reservationRepository.UpdateAsync(reservation, cancellationToken);

        _logger.LogInformation(
            "Booking successfully confirmed. ReservationId: {ReservationId}, UserId: {UserId}, EventId: {EventId}, SeatId: {SeatId}",
            reservation.Id,
            reservation.UserId,
            reservation.EventId,
            reservation.SeatId);

        return Response<ReservationDto>.Success(_mapper.MapToDto(reservation));
    }
}
