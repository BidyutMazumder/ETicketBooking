namespace Booking.Domain.Reservations;

using Booking.Domain.Reservations.ValueObjects;

public sealed class Reservation : AuditableEntity
{
    private Reservation(
        Guid id,
        Guid userId,
        Guid eventId,
        Guid seatId,
        ReservationStatus status,
        DateTime holdExpiresAtUtc,
        PaymentStatus paymentStatus = null!) : base(id)
    {
        UserId = userId;
        EventId = eventId;
        SeatId = seatId;
        Status = status;
        HoldExpiresAtUtc = holdExpiresAtUtc;
        PaymentStatus = paymentStatus ?? PaymentStatus.Pending;
    }

    // Required for EF Core
    private Reservation() { }

    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid SeatId { get; private set; }
    public ReservationStatus Status { get; private set; } = null!;
    public PaymentStatus PaymentStatus { get; private set; } = null!;
    public DateTime HoldExpiresAtUtc { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    public static Reservation Create(Guid userId, Guid eventId, Guid seatId, TimeSpan holdDuration)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId cannot be empty");

        if (eventId == Guid.Empty)
            throw new DomainException("EventId cannot be empty");

        if (seatId == Guid.Empty)
            throw new DomainException("SeatId cannot be empty");

        if (holdDuration <= TimeSpan.Zero)
            throw new DomainException("Hold duration must be positive");

        var holdExpiresAtUtc = DateTime.UtcNow.Add(holdDuration);

        return new Reservation(
            Guid.NewGuid(),
            userId,
            eventId,
            seatId,
            ReservationStatus.Pending,
            holdExpiresAtUtc);
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Pending)
            throw new DomainException($"Cannot confirm a reservation in {Status.Value} status");

        Status = ReservationStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid(string stripePaymentIntentId)
    {
        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Reservation is already paid");

        if (string.IsNullOrWhiteSpace(stripePaymentIntentId))
            throw new DomainException("Stripe Payment Intent ID cannot be empty");

        PaymentStatus = PaymentStatus.Paid;
        StripePaymentIntentId = stripePaymentIntentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        if (PaymentStatus != PaymentStatus.Paid)
            throw new DomainException("Only paid reservations can be refunded");

        PaymentStatus = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ReservationStatus.Cancelled)
            throw new DomainException("Reservation is already cancelled");

        Status = ReservationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsHoldExpired()
    {
        return Status == ReservationStatus.Pending && HoldExpiresAtUtc < DateTime.UtcNow;
    }
}
