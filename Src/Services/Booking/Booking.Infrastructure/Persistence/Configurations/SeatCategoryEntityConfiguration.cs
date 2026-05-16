namespace Booking.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Booking.Domain.Events;

/// <summary>
/// EF Core configuration for the SeatCategory aggregate.
/// Configures the SeatCategoryName and Money value objects as owned types.
/// </summary>
public sealed class SeatCategoryEntityConfiguration : IEntityTypeConfiguration<SeatCategory>
{
    public void Configure(EntityTypeBuilder<SeatCategory> builder)
    {
        builder.ToTable("SeatCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        // Configure SeatCategoryName value object as owned type with flattened column
        builder.OwnsOne(c => c.Name, n =>
        {
            n.Property(x => x.Value)
                .HasColumnName("Name")
                .HasMaxLength(100)
                .IsRequired();

            n.WithOwner();
        });

        // Configure SeatType as string using conversion
        builder.Property(c => c.SeatType)
            .HasConversion(
                v => v.Value,
                v => Booking.Domain.Events.ValueObjects.SeatType.Create(v))
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("SeatType");

        // Configure Money value object as owned type with flattened columns
        builder.OwnsOne(c => c.BasePrice, p =>
        {
            p.Property(m => m.Amount)
                .HasColumnName("BasePriceAmount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            p.Property(m => m.Currency)
                .HasColumnName("BasePriceCurrency")
                .HasMaxLength(3)
                .IsRequired();

            p.WithOwner();
        });

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("Description");

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("IsActive");

        builder.Property(c => c.DiscountPercentage)
            .IsRequired()
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0)
            .HasColumnName("DiscountPercentage");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Create a unique index on Name to prevent duplicates
        builder.HasIndex(c => c.Id)
            .IsUnique();
    }
}
