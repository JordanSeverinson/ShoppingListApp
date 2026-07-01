using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPasswordReset : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PasswordResetToken",
            table: "users",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PasswordResetTokenExpiresAt",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_PasswordResetToken",
            table: "users",
            column: "PasswordResetToken",
            unique: true,
            filter: "\"PasswordResetToken\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_users_PasswordResetToken",
            table: "users");

        migrationBuilder.DropColumn(
            name: "PasswordResetTokenExpiresAt",
            table: "users");

        migrationBuilder.DropColumn(
            name: "PasswordResetToken",
            table: "users");
    }
}
