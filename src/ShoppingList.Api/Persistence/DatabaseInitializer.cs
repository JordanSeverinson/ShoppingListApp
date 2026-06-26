using Microsoft.EntityFrameworkCore;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Persistence;

public static class DatabaseInitializer
{
    public static async Task MigrateAndSeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count(),
                string.Join(", ", pending));
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (!await SchemaIsPresentAsync(db, cancellationToken))
        {
            logger.LogWarning(
                "Database is reachable but shopping list tables are missing. " +
                "Resetting migration history and re-applying migrations.");

            await db.Database.ExecuteSqlRawAsync(
                """DELETE FROM "__EFMigrationsHistory";""",
                cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
        }
        else if (!await ArchiveColumnsArePresentAsync(db, cancellationToken))
        {
            logger.LogWarning(
                "shopping_lists exists but archive columns are missing. Re-applying migrations.");

            await db.Database.MigrateAsync(cancellationToken);
        }

        await DevelopmentDataSeeder.SeedAsync(db, cancellationToken);
    }

    private static async Task<bool> ArchiveColumnsArePresentAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """SELECT "IsArchived" FROM shopping_lists LIMIT 1;""",
                cancellationToken);
            return true;
        }
        catch (Exception ex) when (ColumnMissing(ex))
        {
            return false;
        }
    }

    private static bool ColumnMissing(Exception ex) =>
        ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
        || (ex.InnerException is not null && ColumnMissing(ex.InnerException));

    private static async Task<bool> SchemaIsPresentAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """SELECT 1 FROM shopping_lists LIMIT 1;""",
                cancellationToken);
            return true;
        }
        catch (Exception ex) when (TableMissing(ex))
        {
            return false;
        }
    }

    private static bool TableMissing(Exception ex) =>
        ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
        || (ex.InnerException is not null && TableMissing(ex.InnerException));
}
