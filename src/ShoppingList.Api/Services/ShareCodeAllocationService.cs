using Microsoft.EntityFrameworkCore;
using ShoppingList.Application.Lists;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class ShareCodeAllocationService(ApplicationDbContext db)
{
    public async Task<string> AllocateUniqueShareCodeAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var code = ShareCodeGenerator.Generate();
            var existsInLists = await db.ShoppingLists
                .AsNoTracking()
                .AnyAsync(l => l.ShareCode == code, cancellationToken);

            var existsInRecipes = await db.Recipes
                .AsNoTracking()
                .AnyAsync(r => r.ShareCode == code, cancellationToken);

            if (!existsInLists && !existsInRecipes)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique share code.");
    }
}
