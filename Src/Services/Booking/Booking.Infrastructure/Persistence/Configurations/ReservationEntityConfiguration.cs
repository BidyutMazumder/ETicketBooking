namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class ReservationEntityConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.SeatId)
            .IsRequired();

        builder.Property(e => e.HoldExpiresAtUtc)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        // Map Status as string
        builder.Property(e => e.Status)
            .HasConversion(
                v => v.Value,
                v => Booking.Domain.Reservations.ValueObjects.ReservationStatus.Create(v))
            .IsRequired()
            .HasMaxLength(50);

        // Map PaymentStatus as string
        builder.Property(e => e.PaymentStatus)
            .HasConversion(
                v => v.Value,
                v => Booking.Domain.Reservations.ValueObjects.PaymentStatus.Create(v))
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(Booking.Domain.Reservations.ValueObjects.PaymentStatus.Pending);

        // Payment tracking
        builder.Property(e => e.StripePaymentIntentId)
            .HasMaxLength(256);

        // Optimistic locking with RowVersion (Timestamp)
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .HasColumnName("RowVersion");

        // Create indexes for common queries
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.EventId);
        builder.HasIndex(e => e.SeatId);
        builder.HasIndex(e => new { e.EventId, e.SeatId });
        builder.HasIndex(e => e.HoldExpiresAtUtc);
        builder.HasIndex(e => e.PaymentStatus);
        builder.HasIndex(e => e.StripePaymentIntentId);

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);
    }
}
