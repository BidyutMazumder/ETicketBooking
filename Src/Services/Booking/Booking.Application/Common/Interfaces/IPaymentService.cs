namespace Booking.Application.Common.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Validates a Stripe payment and marks the reservation as paid.
    /// Ensures idempotent processing to prevent double-charging.
    /// </summary>
    /// <param name="reservationId">Reservation ID to mark as paid</param>
    /// <param name="stripePaymentIntentId">Stripe Payment Intent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if payment was successfully processed, false if already processed</returns>
    Task<bool> ValidateAndProcessPaymentAsync(
        Guid reservationId,
        string stripePaymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the payment status for a reservation from Stripe.
    /// </summary>
    /// <param name="stripePaymentIntentId">Stripe Payment Intent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment status ('succeeded', 'pending', 'canceled')</returns>
    Task<string> GetPaymentStatusAsync(
        string stripePaymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a refund for a reservation through Stripe.
    /// </summary>
    /// <param name="reservationId">Reservation ID to refund</param>
    /// <param name="reason">Reason for refund</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if refund was initiated successfully</returns>
    Task<bool> RefundPaymentAsync(
        Guid reservationId,
        string reason,
        CancellationToken cancellationToken = default);
}
