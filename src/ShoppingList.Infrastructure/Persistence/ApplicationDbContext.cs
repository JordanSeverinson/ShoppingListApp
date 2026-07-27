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
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeSharedPermission> RecipeSharedPermissions => Set<RecipeSharedPermission>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<RevokedJwt> RevokedJwts => Set<RevokedJwt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
