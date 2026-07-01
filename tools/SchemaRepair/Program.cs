using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Infrastructure.Persistence;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var settingsPath = Path.Combine(repoRoot, "src", "ShoppingList.Api", "appsettings.Development.local.json");
if (!File.Exists(settingsPath))
{
    Console.Error.WriteLine($"Settings file not found: {settingsPath}");
    return 1;
}

using var json = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath));
var connectionString = json.RootElement
    .GetProperty("ConnectionStrings")
    .GetProperty("DefaultConnection")
    .GetString();

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("DefaultConnection is missing from settings.");
    return 1;
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new ApplicationDbContext(options);

Console.WriteLine("Applying user profile schema repair...");

await db.Database.ExecuteSqlRawAsync(
    """
    ALTER TABLE users ADD COLUMN IF NOT EXISTS "FirstName" character varying(64) NOT NULL DEFAULT 'Friend';
    ALTER TABLE users ADD COLUMN IF NOT EXISTS "FriendCode" character varying(8) NOT NULL DEFAULT 'TEMP0000';
    ALTER TABLE users ADD COLUMN IF NOT EXISTS "LastName" character varying(64);
    ALTER TABLE users ADD COLUMN IF NOT EXISTS "PhoneNumber" character varying(32);
    ALTER TABLE users ADD COLUMN IF NOT EXISTS "PreferredName" character varying(64);
    ALTER TABLE users ADD COLUMN IF NOT EXISTS "Gender" character varying(32);
    """);

await db.Database.ExecuteSqlRawAsync(
    """
    UPDATE users
    SET "FirstName" = COALESCE(
            NULLIF(split_part(COALESCE("DisplayName", ''), ' ', 1), ''),
            'Friend')
    WHERE COALESCE("FirstName", '') = '' OR "FirstName" = 'Friend';
    """);

await db.Database.ExecuteSqlRawAsync(
    """
    UPDATE users
    SET "FriendCode" = upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))
    WHERE "FriendCode" IS NULL OR "FriendCode" = '' OR "FriendCode" = 'TEMP0000';
    """);

await db.Database.ExecuteSqlRawAsync(
    """
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_FriendCode" ON users ("FriendCode");
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_PhoneNumber" ON users ("PhoneNumber") WHERE "PhoneNumber" IS NOT NULL;
    """);

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
    """);

await db.Database.ExecuteSqlRawAsync(
    """
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    SELECT '20260701120000_AddUserProfilesAndFriendships', '10.0.0'
    WHERE NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260701120000_AddUserProfilesAndFriendships');
    """);

await db.Database.ExecuteSqlRawAsync(
    """
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    SELECT '20260701140000_AddUserPreferredNameAndGender', '10.0.0'
    WHERE NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260701140000_AddUserPreferredNameAndGender');
    """);

await db.Database.ExecuteSqlRawAsync(
    """SELECT "FirstName", "FriendCode", "Gender" FROM users LIMIT 1;""");

Console.WriteLine("Schema repair completed successfully.");
return 0;
