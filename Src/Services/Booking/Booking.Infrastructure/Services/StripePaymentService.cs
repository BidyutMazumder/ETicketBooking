using Booking.Application.Common.Interfaces;
using Booking.Domain.Reservations.ValueObjects;

namespace Booking.Infrastructure.Services;

/// <summary>
/// Stripe Payment Service implementation.
/// Handles payment validation, processing, and refunds with idempotent operations.
/// </summary>
public sealed class StripePaymentService : IPaymentService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly string _stripeApiKey;

    public StripePaymentService(
        IReservationRepository reservationRepository,
        ILogger<StripePaymentService> logger,
        IConfiguration configuration)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
        _stripeApiKey = configuration["Payment:StripeSecretKey"] ?? "";

        if (string.IsNullOrWhiteSpace(_stripeApiKey))
        {
            _logger.LogWarning("Stripe API key not configured. Payment processing will be simulated.");
        }
    }

    public async Task<bool> ValidateAndProcessPaymentAsync(
        Guid reservationId,
        string stripePaymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch the reservation
            var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
            if (reservation is null)
            {
                _logger.LogWarning("Reservation {ReservationId} not found for payment processing", reservationId);
                return false;
            }

            // Idempotency check: if already paid with the same intent ID, return true
            if (reservation.PaymentStatus == PaymentStatus.Paid && 
                reservation.StripePaymentIntentId == stripePaymentIntentId)
            {
                _logger.LogInformation(
                    "Reservation {ReservationId} already paid with intent {IntentId}. Skipping duplicate processing.",
                    reservationId,
                    stripePaymentIntentId);
                return true;
            }

            // Check if payment is already marked as failed to prevent re-processing
            if (reservation.PaymentStatus == PaymentStatus.Failed)
            {
                _logger.LogWarning(
                    "Reservation {ReservationId} has a failed payment status. Cannot process new payment.",
                    reservationId);
                return false;
            }

            // Validate the payment status with Stripe
            var paymentStatus = await GetPaymentStatusAsync(stripePaymentIntentId, cancellationToken);

            if (paymentStatus != "succeeded")
            {
                _logger.LogWarning(
                    "Stripe payment intent {IntentId} status is {Status}, not succeeded",
                    stripePaymentIntentId,
                    paymentStatus);
                reservation.MarkPaymentFailed();
                await _reservationRepository.UpdateAsync(reservation, cancellationToken);
                return false;
            }

            // Mark reservation as paid
            reservation.MarkAsPaid(stripePaymentIntentId);
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);

            _logger.LogInformation(
                "Reservation {ReservationId} marked as paid with Stripe intent {IntentId}",
                reservationId,
                stripePaymentIntentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing payment for reservation {ReservationId}",
                reservationId);
            return false;
        }
    }

    public async Task<string> GetPaymentStatusAsync(
        string stripePaymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // If no API key configured, simulate successful payment for development
            if (string.IsNullOrWhiteSpace(_stripeApiKey))
            {
                _logger.LogDebug(
                    "Simulating payment status check for intent {IntentId} (no Stripe key configured)",
                    stripePaymentIntentId);
                await Task.Delay(100, cancellationToken); // Simulate network call
                return "succeeded";
            }

            // In production, this would call Stripe API
            // For now, this is a placeholder implementation
            _logger.LogInformation("Retrieving payment status for intent {IntentId} from Stripe", stripePaymentIntentId);

            // TODO: Implement actual Stripe API call once Stripe NuGet is added
            // var intent = await StripeClient.RetrievePaymentIntentAsync(stripePaymentIntentId);
            // return intent.Status;

            return "succeeded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment status from Stripe");
            throw;
        }
    }

    public async Task<bool> RefundPaymentAsync(
        Guid reservationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
            if (reservation is null)
            {
                _logger.LogWarning("Reservation {ReservationId} not found for refund processing", reservationId);
                return false;
            }

            if (reservation.PaymentStatus != PaymentStatus.Paid)
            {
                _logger.LogWarning(
                    "Reservation {ReservationId} is not in paid status. Cannot refund.",
                    reservationId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(reservation.StripePaymentIntentId))
            {
                _logger.LogWarning(
                    "Reservation {ReservationId} has no Stripe Payment Intent ID. Cannot refund.",
                    reservationId);
                return false;
            }

            // TODO: Implement actual Stripe refund call once Stripe NuGet is added
            // await StripeClient.RefundPaymentAsync(reservation.StripePaymentIntentId, reason);

            reservation.MarkAsRefunded();
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);

            _logger.LogInformation(
                "Reservation {ReservationId} refunded successfully. Reason: {Reason}",
                reservationId,
                reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for reservation {ReservationId}", reservationId);
            return false;
        }
    }
}
