using Microsoft.EntityFrameworkCore;
using ShoppingList.Domain.Entities;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ShoppingListEntity> ShoppingLists => Set<ShoppingListEntity>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<SharedPermission> SharedPermissions => Set<SharedPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
