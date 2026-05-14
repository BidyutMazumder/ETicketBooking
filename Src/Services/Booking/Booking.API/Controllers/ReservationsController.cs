using Booking.Application.Features.Events.Commands.CreateEvent;
using Booking.Application.Features.Events.Queries.GetAvailableSeats;
using Booking.Application.Features.Reservations.Commands.ConfirmBooking;
using Booking.Application.Features.Reservations.Commands.ConfirmPayment;
using Booking.Application.Features.Reservations.Commands.HoldSeat;

namespace Booking.API.Controllers;

/// <summary>
/// Reservation Management Endpoints
/// Handles seat reservations and booking lifecycle
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ReservationsController : BaseController
{
    public ReservationsController(ISender sender) : base(sender) { }

    /// <summary>
    /// Hold a seat for a user (Add to Cart)
    /// </summary>
    /// <remarks>
    /// Places a hold on a specific seat for 10 minutes. During this time, the seat is reserved
    /// for the user to complete checkout. If payment is not confirmed within 10 minutes,
    /// the hold is automatically released.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/reservations/hold
    ///     {
    ///        "eventId": "550e8400-e29b-41d4-a716-446655440000",
    ///        "seatId": "660e8400-e29b-41d4-a716-446655440001",
    ///        "userId": "770e8400-e29b-41d4-a716-446655440002"
    ///     }
    /// </remarks>
    /// <param name="command">Seat hold details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reservation with hold expiry time</returns>
    /// <response code="201">Seat held successfully</response>
    /// <response code="400">Seat already held/sold or invalid input</response>
    /// <response code="409">Concurrency conflict (race condition)</response>
    /// <response code="500">Server error</response>
    [HttpPost("hold")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HoldSeat(
        [FromBody] HoldSeatCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(HoldSeat), new { id = result.Data?.Id }, result)
            : BadRequest(result);
    }

    /// <summary>
    /// Confirm a booking (After Payment)
    /// </summary>
    /// <remarks>
    /// Confirms a previously held seat after successful payment processing.
    /// This transitions the reservation from "Pending" to "Confirmed" state
    /// and the seat from "Held" to "Reserved" state.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/reservations/confirm
    ///     {
    ///        "reservationId": "880e8400-e29b-41d4-a716-446655440003"
    ///     }
    /// </remarks>
    /// <param name="command">Confirmation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmed reservation</returns>
    /// <response code="200">Booking confirmed successfully</response>
    /// <response code="400">Reservation not found or already expired</response>
    /// <response code="500">Server error</response>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmBooking(
        [FromBody] ConfirmBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>
    /// Confirm payment for a reservation (Stripe Integration)
    /// </summary>
    /// <remarks>
    /// Validates a Stripe payment and marks the reservation as paid.
    /// Ensures idempotent processing to prevent double-charging.
    /// This transition updates the payment status but does not yet confirm the booking.
    /// Typically followed by the /confirm endpoint to finalize the reservation.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/reservations/confirm-payment
    ///     {
    ///        "reservationId": "880e8400-e29b-41d4-a716-446655440003",
    ///        "stripePaymentIntentId": "pi_1234567890abcdef"
    ///     }
    /// </remarks>
    /// <param name="command">Payment confirmation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reservation with updated payment status</returns>
    /// <response code="200">Payment confirmed successfully</response>
    /// <response code="400">Payment validation failed or reservation not found</response>
    /// <response code="409">Concurrency conflict or payment already processed</response>
    /// <response code="500">Server error</response>
    [HttpPost("confirm-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result)
            : BadRequest(result);
    }
}
