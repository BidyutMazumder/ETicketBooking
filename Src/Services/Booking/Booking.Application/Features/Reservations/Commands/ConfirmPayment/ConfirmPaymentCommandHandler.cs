using Shared.Kernel.Domain.Exceptions;

namespace Booking.Application.Features.Reservations.Commands.ConfirmPayment;

/// <summary>
/// Handles payment confirmation for reservations with strict idempotency.
/// Uses pessimistic locking to prevent concurrent payment processing.
/// Ensures exactly-once payment semantics through reservation-level uniqueness.
/// </summary>
public sealed class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, Response<ReservationDto>>
{
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 50;

    private readonly IPaymentService _paymentService;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationMapper _mapper;
    private readonly ILogger<ConfirmPaymentCommandHandler> _logger;

    public ConfirmPaymentCommandHandler(
        IPaymentService paymentService,
        IReservationRepository reservationRepository,
        IReservationMapper mapper,
        ILogger<ConfirmPaymentCommandHandler> logger)
    {
        _paymentService = paymentService;
        _reservationRepository = reservationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async ValueTask<Response<ReservationDto>> Handle(
        ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                return await ProcessPaymentWithIdempotencyAsync(request, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrency conflict on attempt {Attempt}/{MaxAttempts} for payment confirmation. ReservationId: {ReservationId}",
                    attempt + 1,
                    MaxRetryAttempts,
                    request.ReservationId);

                if (attempt == MaxRetryAttempts - 1)
                {
                    _logger.LogError(
                        "Max retry attempts exceeded for payment confirmation. ReservationId: {ReservationId}",
                        request.ReservationId);

                    return Response<ReservationDto>.Failure(
                        new Error("Payment.Conflict", "Payment processing encountered a conflict. Please refresh and try again."));
                }

                await Task.Delay(RetryDelayMs * (int)Math.Pow(2, attempt), cancellationToken);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Domain validation error during payment confirmation. ReservationId: {ReservationId}",
                    request.ReservationId);

                return Response<ReservationDto>.Failure(
                    new Error("Payment.ConfirmFailed", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error confirming payment for reservation {ReservationId}",
                    request.ReservationId);

                return Response<ReservationDto>.Failure(
                    new Error("Payment.Error", "An unexpected error occurred while processing payment"));
            }
        }

        return Response<ReservationDto>.Failure(
            new Error("Payment.Error", "Payment confirmation failed after multiple attempts"));
    }

    private async ValueTask<Response<ReservationDto>> ProcessPaymentWithIdempotencyAsync(
        ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        // Fetch reservation with pessimistic lock to prevent concurrent payment processing
        var reservation = await _reservationRepository.GetByIdWithLockAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", request.ReservationId);
            return Response<ReservationDto>.Failure(
                new Error("Reservation.NotFound", "Reservation not found"));
        }

        // Idempotency check: if already paid with the same intent ID, return success
        if (reservation.PaymentStatus == PaymentStatus.Paid &&
            reservation.StripePaymentIntentId == request.StripePaymentIntentId)
        {
            _logger.LogInformation(
                "Payment already confirmed for reservation {ReservationId} with intent {IntentId}. Returning success for idempotent retry.",
                request.ReservationId,
                request.StripePaymentIntentId);

            return Response<ReservationDto>.Success(_mapper.MapToDto(reservation));
        }

        // Prevent processing if payment is already marked as paid with a different intent
        if (reservation.PaymentStatus == PaymentStatus.Paid &&
            reservation.StripePaymentIntentId != request.StripePaymentIntentId)
        {
            _logger.LogError(
                "Attempted to confirm payment with different intent ID for already-paid reservation. ReservationId: {ReservationId}, ExistingIntentId: {ExistingIntentId}, NewIntentId: {NewIntentId}",
                request.ReservationId,
                reservation.StripePaymentIntentId,
                request.StripePaymentIntentId);

            return Response<ReservationDto>.Failure(
                new Error("Payment.AlreadyConfirmed", "Payment has already been confirmed for this reservation"));
        }

        // Check if hold has expired
        if (reservation.IsHoldExpired())
        {
            _logger.LogWarning(
                "Attempt to confirm payment for expired hold. ReservationId: {ReservationId}, ExpiresAt: {ExpiresAt}",
                request.ReservationId,
                reservation.HoldExpiresAtUtc);

            return Response<ReservationDto>.Failure(
                new Error("Reservation.HoldExpired", "Hold has expired. Please place a new reservation."));
        }

        // Validate and process the payment through the payment service
        var paymentProcessed = await _paymentService.ValidateAndProcessPaymentAsync(
            request.ReservationId,
            request.StripePaymentIntentId,
            cancellationToken);

        if (!paymentProcessed)
        {
            _logger.LogWarning(
                "Payment processing failed for reservation {ReservationId} with intent {IntentId}",
                request.ReservationId,
                request.StripePaymentIntentId);

            return Response<ReservationDto>.Failure(
                new Error("Payment.Failed", "Payment could not be processed. Please verify your payment details and try again."));
        }

        // Refresh the reservation to get the updated payment status from the database
        var updatedReservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (updatedReservation is null)
        {
            return Response<ReservationDto>.Failure(
                new Error("Reservation.NotFound", "Reservation not found after payment processing"));
        }

        _logger.LogInformation(
            "Payment successfully confirmed for reservation {ReservationId} with Stripe intent {IntentId}",
            request.ReservationId,
            request.StripePaymentIntentId);

        return Response<ReservationDto>.Success(_mapper.MapToDto(updatedReservation));
    }
}
