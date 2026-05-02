using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;
public sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.OwnsOne(e => e.Email, p =>
        {
            p.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Email");

            p.HasIndex(e => e.Value)
                .IsUnique();
        });

        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Role)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Complex property mapping for FullName
        builder.OwnsOne(e => e.Name, nav =>
        {
            nav.Property(n => n.FirstName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("FirstName");

            nav.Property(n => n.LastName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("LastName");
        });

        

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);
    }
}
