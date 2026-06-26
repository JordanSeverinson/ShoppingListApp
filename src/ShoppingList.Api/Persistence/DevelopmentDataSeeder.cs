using Microsoft.EntityFrameworkCore;
using ShoppingList.Application.Lists;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Api.Persistence;

public static class DevelopmentDataSeeder
{
    public static readonly Guid DemoListId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid HistoricalListId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DemoUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = DemoUserId,
            Email = "demo@shoppinglist.local",
            PasswordHash = "dev-only",
            DisplayName = "Demo User",
            CreatedAt = DateTime.UtcNow
        });

        db.ShoppingLists.Add(new ShoppingListEntity
        {
            Id = DemoListId,
            Name = "Shared Groceries",
            OwnerId = DemoUserId,
            ShareCode = "K7HN3M9P2XQ",
            CreatedAt = DateTime.UtcNow,
            Items =
            [
                new ListItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Bananas",
                    Category = "Produce",
                    Quantity = "1 bunch",
                    SortOrder = 1,
                    IsChecked = false,
                    CreatedAt = DateTime.UtcNow
                },
                new ListItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Milk",
                    Category = "Dairy",
                    Quantity = "1 gallon",
                    SortOrder = 2,
                    IsChecked = true,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        });

        db.ShoppingLists.Add(new ShoppingListEntity
        {
            Id = HistoricalListId,
            Name = "Last week's shop",
            OwnerId = DemoUserId,
            ShareCode = ShareCodeGenerator.Generate(),
            IsArchived = true,
            ArchivedAt = DateTime.UtcNow.AddDays(-7),
            CreatedAt = DateTime.UtcNow.AddDays(-14),
            UpdatedAt = DateTime.UtcNow.AddDays(-7),
            Items =
            [
                new ListItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Bread",
                    Category = "Bakery",
                    Quantity = "1 loaf",
                    SortOrder = 1,
                    IsChecked = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                },
                new ListItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Eggs",
                    Category = "Dairy",
                    Quantity = "12",
                    SortOrder = 2,
                    IsChecked = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                }
            ]
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
