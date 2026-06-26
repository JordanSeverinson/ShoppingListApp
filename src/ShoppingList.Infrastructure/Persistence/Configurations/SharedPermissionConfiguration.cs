using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class SharedPermissionConfiguration : IEntityTypeConfiguration<SharedPermission>
{
    public void Configure(EntityTypeBuilder<SharedPermission> builder)
    {
        builder.ToTable("shared_permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PermissionLevel)
            .HasConversion(
                level => level.ToString(),
                value => Enum.Parse<PermissionLevel>(value))
            .HasMaxLength(20);

        builder.HasIndex(p => new { p.UserId, p.ShoppingListId })
            .IsUnique();

        builder.HasIndex(p => p.ShoppingListId);
    }
}
