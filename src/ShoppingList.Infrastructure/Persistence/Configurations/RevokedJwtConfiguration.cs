using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class RevokedJwtConfiguration : IEntityTypeConfiguration<RevokedJwt>
{
    public void Configure(EntityTypeBuilder<RevokedJwt> builder)
    {
        builder.ToTable("revoked_jwts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(r => r.TokenHash)
            .IsUnique();

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.HasIndex(r => r.ExpiresAt);

        builder.Property(r => r.CreatedAt)
            .IsRequired();
    }
}
