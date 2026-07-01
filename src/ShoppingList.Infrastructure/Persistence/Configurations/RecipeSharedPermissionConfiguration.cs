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

        builder.Property(p => p.Status)
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<ListShareStatus>(value))
            .HasMaxLength(20)
            .HasDefaultValue(ListShareStatus.Pending)
            .ValueGeneratedNever();

        builder.HasIndex(p => p.Status);

        builder.HasIndex(p => new { p.UserId, p.RecipeId })
            .IsUnique();

        builder.HasIndex(p => p.RecipeId);
    }
}
