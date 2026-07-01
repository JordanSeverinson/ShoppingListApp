using Microsoft.EntityFrameworkCore;
using ShoppingList.Application.Users;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class FriendCodeAllocationService(ApplicationDbContext db)
{
    public async Task<string> AllocateUniqueFriendCodeAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var code = FriendCodeGenerator.Generate();
            var exists = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.FriendCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique friend code.");
    }
}
