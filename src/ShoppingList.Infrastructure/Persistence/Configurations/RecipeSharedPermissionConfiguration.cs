using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class RecipeSharedPermissionConfiguration : IEntityTypeConfiguration<RecipeSharedPermission>
{
    public void Configure(EntityTypeBuilder<RecipeSharedPermission> builder)
    {
        builder.ToTable("recipe_shared_permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PermissionLevel)
            .HasConversion(
                level => level.ToString(),
                value => Enum.Parse<PermissionLevel>(value))
            .HasMaxLength(20);

        builder.HasIndex(p => new { p.UserId, p.RecipeId })
            .IsUnique();

        builder.HasIndex(p => p.RecipeId);
    }
}
