namespace Booking.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Event aggregate and its child entities.
/// Configures the Money value object as owned type with flattened columns.
/// </summary>
public sealed class EventEntityConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.StartDateTime)
            .IsRequired();

        builder.Property(e => e.VenueName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        // Configure Seats as owned entities (children of Event)
        builder.OwnsMany(e => e.Seats, s =>
        {
            s.ToTable("Seats");
            s.WithOwner().HasForeignKey("EventId");
            s.HasKey("Id");

            s.Property(x => x.Row)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Row");

            s.Property(x => x.Number)
                .IsRequired()
                .HasColumnName("Number");

            s.Property(x => x.HeldUntilUtc)
                .HasColumnName("HeldUntilUtc");

            s.Property(x => x.RowVersion)
                .IsRowVersion()
                .HasColumnName("RowVersion");

            // Configure Money value object as owned type with flattened columns
            s.OwnsOne(x => x.Price, p =>
            {
                p.Property(m => m.Amount)
                    .HasColumnName("PriceAmount")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                p.Property(m => m.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();

                p.WithOwner();
            });

            // Map Type as string
            s.Property(x => x.Type)
                .HasConversion(
                    v => v.Value,
                    v => Booking.Domain.Events.ValueObjects.SeatType.Create(v))
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Type");

            // Map Status as string
            s.Property(x => x.Status)
                .HasConversion(
                    v => v.Value,
                    v => Booking.Domain.Events.ValueObjects.SeatStatus.Create(v))
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Status");

            // Ensure seat uniqueness within an event
            s.HasIndex("Row", "Number").IsUnique();
        });

        // Ignore domain events (handled separately by the domain event dispatcher)
        builder.Ignore(e => e.DomainEvents);
    }
}
