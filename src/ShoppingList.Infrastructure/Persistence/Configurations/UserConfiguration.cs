using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(u => u.DisplayName)
            .HasMaxLength(128);

        builder.HasMany(u => u.OwnedLists)
            .WithOne(l => l.Owner)
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.SharedPermissions)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
