using Microsoft.EntityFrameworkCore;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class UserSecurityStampService(ApplicationDbContext db)
{
    public async Task<Guid> GetStampAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.SecurityStamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Guid RotateStamp(User user)
    {
        user.SecurityStamp = Guid.NewGuid();
        return user.SecurityStamp;
    }
}
