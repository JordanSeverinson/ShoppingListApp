using Microsoft.EntityFrameworkCore;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Persistence;

public static class DatabaseInitializer
{
    public static async Task MigrateAsync(
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
        else if (!await UserProfileColumnsArePresentAsync(db, cancellationToken))
        {
            logger.LogWarning(
                "users table exists but profile columns are missing. Applying profile schema repair.");

            await ApplyUserProfileSchemaRepairAsync(db, cancellationToken);
        }
        else if (!await EmailVerificationColumnsArePresentAsync(db, cancellationToken))
        {
            logger.LogWarning(
                "users table exists but email verification columns are missing. Applying email verification repair.");

            await ApplyEmailVerificationSchemaRepairAsync(db, cancellationToken);
        }

        await RepairMisacceptedListSharesAsync(db, cancellationToken);
        await RepairMisacceptedRecipeSharesAsync(db, cancellationToken);
    }

    private static async Task ApplyUserProfileSchemaRepairAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "FirstName" character varying(64) NOT NULL DEFAULT 'Friend';
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "FriendCode" character varying(8) NOT NULL DEFAULT 'TEMP0000';
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "LastName" character varying(64);
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "PhoneNumber" character varying(32);
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "PreferredName" character varying(64);
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "Gender" character varying(32);
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET "FirstName" = COALESCE(
                    NULLIF(split_part(COALESCE("DisplayName", ''), ' ', 1), ''),
                    'Friend')
            WHERE COALESCE("FirstName", '') = '' OR "FirstName" = 'Friend';
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET "FriendCode" = upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))
            WHERE "FriendCode" IS NULL OR "FriendCode" = '' OR "FriendCode" = 'TEMP0000';
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_FriendCode" ON users ("FriendCode");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_PhoneNumber" ON users ("PhoneNumber") WHERE "PhoneNumber" IS NOT NULL;
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS friendships (
                "Id" uuid NOT NULL,
                "RequesterId" uuid NOT NULL,
                "AddresseeId" uuid NOT NULL,
                "Status" character varying(20) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                CONSTRAINT "PK_friendships" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_friendships_users_AddresseeId" FOREIGN KEY ("AddresseeId") REFERENCES users ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_friendships_users_RequesterId" FOREIGN KEY ("RequesterId") REFERENCES users ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_friendships_AddresseeId" ON friendships ("AddresseeId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_friendships_RequesterId_AddresseeId" ON friendships ("RequesterId", "AddresseeId");
            CREATE INDEX IF NOT EXISTS "IX_friendships_Status" ON friendships ("Status");
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260701120000_AddUserProfilesAndFriendships', '10.0.0'
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260701120000_AddUserProfilesAndFriendships');
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260701140000_AddUserPreferredNameAndGender', '10.0.0'
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260701140000_AddUserPreferredNameAndGender');
            """,
            cancellationToken);
    }

    private static async Task ApplyEmailVerificationSchemaRepairAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "EmailVerified" boolean NOT NULL DEFAULT false;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "EmailVerificationToken" character varying(128);
            ALTER TABLE users ADD COLUMN IF NOT EXISTS "EmailVerificationTokenExpiresAt" timestamp with time zone;
            UPDATE users SET "EmailVerified" = true;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_EmailVerificationToken" ON users ("EmailVerificationToken") WHERE "EmailVerificationToken" IS NOT NULL;
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260702120000_AddEmailVerification', '10.0.0'
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260702120000_AddEmailVerification');
            """,
            cancellationToken);
    }

    private static async Task<bool> EmailVerificationColumnsArePresentAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """SELECT "EmailVerified", "EmailVerificationToken" FROM users LIMIT 1;""",
                cancellationToken);
            return true;
        }
        catch (Exception ex) when (ColumnMissing(ex))
        {
            return false;
        }
    }

    private static async Task<bool> UserProfileColumnsArePresentAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """SELECT "FirstName", "FriendCode", "Gender" FROM users LIMIT 1;""",
                cancellationToken);
            return true;
        }
        catch (Exception ex) when (ColumnMissing(ex))
        {
            return false;
        }
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

    private static async Task RepairMisacceptedListSharesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE shared_permissions
                    ALTER COLUMN "Status" SET DEFAULT 'Pending';
                UPDATE shared_permissions
                SET "Status" = 'Pending'
                WHERE "GrantedAt" IS NULL AND "Status" = 'Accepted';
                """,
                cancellationToken);
        }
        catch (Exception ex) when (ColumnMissing(ex) || TableMissing(ex))
        {
            // Status column not present yet; migrations will handle it.
        }
    }

    private static async Task RepairMisacceptedRecipeSharesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE recipe_shared_permissions
                    ALTER COLUMN "Status" SET DEFAULT 'Pending';
                UPDATE recipe_shared_permissions
                SET "Status" = 'Pending'
                WHERE "GrantedAt" IS NULL AND "Status" = 'Accepted';
                """,
                cancellationToken);
        }
        catch (Exception ex) when (ColumnMissing(ex) || TableMissing(ex))
        {
            // Status column not present yet; migrations will handle it.
        }
    }
}
