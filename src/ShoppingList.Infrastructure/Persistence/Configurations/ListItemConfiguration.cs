using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingList.Domain.Entities;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class ListItemConfiguration : IEntityTypeConfiguration<ListItem>
{
    public void Configure(EntityTypeBuilder<ListItem> builder)
    {
        builder.ToTable("list_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(i => i.Quantity)
            .HasMaxLength(100);

        builder.Property(i => i.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(i => new { i.ShoppingListId, i.SortOrder });
    }
}
