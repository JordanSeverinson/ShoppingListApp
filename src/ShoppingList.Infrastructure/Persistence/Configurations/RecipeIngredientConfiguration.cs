using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("recipe_ingredients");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(i => i.Quantity)
            .HasMaxLength(100);

        builder.Property(i => i.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Section)
            .HasMaxLength(100);

        builder.HasIndex(i => new { i.RecipeId, i.SortOrder });
    }
}
