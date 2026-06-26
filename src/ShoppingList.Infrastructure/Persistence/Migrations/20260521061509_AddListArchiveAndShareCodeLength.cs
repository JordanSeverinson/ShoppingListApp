using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListArchiveAndShareCodeLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShareCode",
                table: "shopping_lists",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "shopping_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "shopping_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_shopping_lists_OwnerId_IsArchived",
                table: "shopping_lists",
                columns: new[] { "OwnerId", "IsArchived" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shopping_lists_OwnerId_IsArchived",
                table: "shopping_lists");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "shopping_lists");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "shopping_lists");

            migrationBuilder.AlterColumn<string>(
                name: "ShareCode",
                table: "shopping_lists",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);
        }
    }
}
