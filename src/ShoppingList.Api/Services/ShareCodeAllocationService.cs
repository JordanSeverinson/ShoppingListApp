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
            var exists = await db.ShoppingLists
                .AsNoTracking()
                .AnyAsync(l => l.ShareCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique share code.");
    }
}
