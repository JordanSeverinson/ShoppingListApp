using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEmailVerification : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EmailVerified",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "EmailVerificationToken",
            table: "users",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EmailVerificationTokenExpiresAt",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("""UPDATE users SET "EmailVerified" = true;""");

        migrationBuilder.CreateIndex(
            name: "IX_users_EmailVerificationToken",
            table: "users",
            column: "EmailVerificationToken",
            unique: true,
            filter: "\"EmailVerificationToken\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_users_EmailVerificationToken",
            table: "users");

        migrationBuilder.DropColumn(
            name: "EmailVerificationTokenExpiresAt",
            table: "users");

        migrationBuilder.DropColumn(
            name: "EmailVerificationToken",
            table: "users");

        migrationBuilder.DropColumn(
            name: "EmailVerified",
            table: "users");
    }
}
