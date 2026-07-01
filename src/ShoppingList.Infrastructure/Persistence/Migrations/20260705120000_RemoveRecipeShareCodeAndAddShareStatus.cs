using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RemoveRecipeShareCodeAndAddShareStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "recipe_shared_permissions",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Accepted");

        migrationBuilder.AddColumn<Guid>(
            name: "InvitedByUserId",
            table: "recipe_shared_permissions",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE recipe_shared_permissions rsp
            SET "InvitedByUserId" = r."OwnerId"
            FROM recipes r
            WHERE rsp."RecipeId" = r."Id" AND rsp."InvitedByUserId" IS NULL;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "InvitedByUserId",
            table: "recipe_shared_permissions",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "GrantedAt",
            table: "recipe_shared_permissions",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_shared_permissions_Status",
            table: "recipe_shared_permissions",
            column: "Status");

        migrationBuilder.DropIndex(
            name: "IX_recipes_ShareCode",
            table: "recipes");

        migrationBuilder.DropColumn(
            name: "ShareCode",
            table: "recipes");

        migrationBuilder.Sql(
            """
            ALTER TABLE recipe_shared_permissions ALTER COLUMN "Status" SET DEFAULT 'Pending';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ShareCode",
            table: "recipes",
            type: "character varying(11)",
            maxLength: 11,
            nullable: false,
            defaultValue: "TEMP0000000");

        migrationBuilder.CreateIndex(
            name: "IX_recipes_ShareCode",
            table: "recipes",
            column: "ShareCode",
            unique: true);

        migrationBuilder.DropIndex(
            name: "IX_recipe_shared_permissions_Status",
            table: "recipe_shared_permissions");

        migrationBuilder.DropColumn(
            name: "InvitedByUserId",
            table: "recipe_shared_permissions");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "recipe_shared_permissions");

        migrationBuilder.AlterColumn<DateTime>(
            name: "GrantedAt",
            table: "recipe_shared_permissions",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);
    }
}
