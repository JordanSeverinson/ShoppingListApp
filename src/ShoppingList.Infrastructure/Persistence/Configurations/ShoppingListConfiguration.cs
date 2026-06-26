using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Infrastructure.Persistence.Configurations;

public class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingListEntity>
{
    public void Configure(EntityTypeBuilder<ShoppingListEntity> builder)
    {
        builder.ToTable("shopping_lists");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.ShareCode)
            .IsRequired()
            .HasMaxLength(11);

        builder.HasIndex(l => l.ShareCode)
            .IsUnique();

        builder.HasIndex(l => l.OwnerId);

        builder.HasIndex(l => new { l.OwnerId, l.IsArchived });

        builder.Property(l => l.IsArchived)
            .HasDefaultValue(false);

        builder.HasMany(l => l.Items)
            .WithOne(i => i.ShoppingList)
            .HasForeignKey(i => i.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.SharedPermissions)
            .WithOne(p => p.ShoppingList)
            .HasForeignKey(p => p.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
