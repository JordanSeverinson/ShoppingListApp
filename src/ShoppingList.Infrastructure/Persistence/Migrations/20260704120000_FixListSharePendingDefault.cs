using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class FixListSharePendingDefault : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE shared_permissions ALTER COLUMN "Status" SET DEFAULT 'Pending';
            UPDATE shared_permissions
            SET "Status" = 'Pending'
            WHERE "GrantedAt" IS NULL AND "Status" = 'Accepted';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE shared_permissions ALTER COLUMN "Status" SET DEFAULT 'Accepted';
            """);
    }
}
