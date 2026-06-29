using Microsoft.EntityFrameworkCore;
using ShoppingList.Application.Lists;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Api.Persistence;

public static class DevelopmentDataSeeder
{
    public static readonly Guid DemoListId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DemoArchivedListId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DemoRecipeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
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
            Id = DemoArchivedListId,
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

        db.Recipes.Add(new Recipe
        {
            Id = DemoRecipeId,
            Name = "Weeknight Pasta",
            OwnerId = DemoUserId,
            ShareCode = "RCP7HN3M9P2",
            CreatedAt = DateTime.UtcNow,
            Ingredients =
            [
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    Name = "Spaghetti",
                    Category = "Pantry",
                    Quantity = "1 lb",
                    SortOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    Name = "Marinara sauce",
                    Category = "Pantry",
                    Quantity = "24 oz",
                    SortOrder = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    Name = "Parmesan",
                    Category = "Dairy",
                    Quantity = "1/2 cup",
                    SortOrder = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    Name = "Basil",
                    Category = "Produce",
                    Quantity = "1 bunch",
                    SortOrder = 4,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
