using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.ShareCode)
            .IsRequired()
            .HasMaxLength(11);

        builder.HasIndex(r => r.ShareCode)
            .IsUnique();

        builder.HasIndex(r => r.OwnerId);

        builder.HasMany(r => r.Ingredients)
            .WithOne(i => i.Recipe)
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.SharedPermissions)
            .WithOne(p => p.Recipe)
            .HasForeignKey(p => p.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
