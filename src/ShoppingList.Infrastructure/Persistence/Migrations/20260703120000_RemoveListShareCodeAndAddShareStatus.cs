using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RemoveListShareCodeAndAddShareStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "shared_permissions",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Accepted");

        migrationBuilder.AddColumn<Guid>(
            name: "InvitedByUserId",
            table: "shared_permissions",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE shared_permissions sp
            SET "InvitedByUserId" = sl."OwnerId"
            FROM shopping_lists sl
            WHERE sp."ShoppingListId" = sl."Id" AND sp."InvitedByUserId" IS NULL;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "InvitedByUserId",
            table: "shared_permissions",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "GrantedAt",
            table: "shared_permissions",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.CreateIndex(
            name: "IX_shared_permissions_Status",
            table: "shared_permissions",
            column: "Status");

        migrationBuilder.DropIndex(
            name: "IX_shopping_lists_ShareCode",
            table: "shopping_lists");

        migrationBuilder.DropColumn(
            name: "ShareCode",
            table: "shopping_lists");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ShareCode",
            table: "shopping_lists",
            type: "character varying(11)",
            maxLength: 11,
            nullable: false,
            defaultValue: "TEMP0000000");

        migrationBuilder.CreateIndex(
            name: "IX_shopping_lists_ShareCode",
            table: "shopping_lists",
            column: "ShareCode",
            unique: true);

        migrationBuilder.DropIndex(
            name: "IX_shared_permissions_Status",
            table: "shared_permissions");

        migrationBuilder.DropColumn(
            name: "InvitedByUserId",
            table: "shared_permissions");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "shared_permissions");

        migrationBuilder.AlterColumn<DateTime>(
            name: "GrantedAt",
            table: "shared_permissions",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);
    }
}
