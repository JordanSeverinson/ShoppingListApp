using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "shopping_lists",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                ShareCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shopping_lists", x => x.Id);
                table.ForeignKey(
                    name: "FK_shopping_lists_users_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "list_items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ShoppingListId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Quantity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IsChecked = table.Column<bool>(type: "boolean", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_list_items", x => x.Id);
                table.ForeignKey(
                    name: "FK_list_items_shopping_lists_ShoppingListId",
                    column: x => x.ShoppingListId,
                    principalTable: "shopping_lists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "shared_permissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ShoppingListId = table.Column<Guid>(type: "uuid", nullable: false),
                PermissionLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shared_permissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_shared_permissions_shopping_lists_ShoppingListId",
                    column: x => x.ShoppingListId,
                    principalTable: "shopping_lists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_shared_permissions_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_list_items_ShoppingListId_SortOrder",
            table: "list_items",
            columns: new[] { "ShoppingListId", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_shared_permissions_ShoppingListId",
            table: "shared_permissions",
            column: "ShoppingListId");

        migrationBuilder.CreateIndex(
            name: "IX_shared_permissions_UserId_ShoppingListId",
            table: "shared_permissions",
            columns: new[] { "UserId", "ShoppingListId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_shopping_lists_OwnerId",
            table: "shopping_lists",
            column: "OwnerId");

        migrationBuilder.CreateIndex(
            name: "IX_shopping_lists_ShareCode",
            table: "shopping_lists",
            column: "ShareCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_Email",
            table: "users",
            column: "Email",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "list_items");
        migrationBuilder.DropTable(name: "shared_permissions");
        migrationBuilder.DropTable(name: "shopping_lists");
        migrationBuilder.DropTable(name: "users");
    }
}
