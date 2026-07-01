using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("recipe_steps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(s => new { s.RecipeId, s.SortOrder });
    }
}
