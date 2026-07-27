using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRevokedJwts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "revoked_jwts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_revoked_jwts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_revoked_jwts_ExpiresAt",
            table: "revoked_jwts",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_revoked_jwts_TokenHash",
            table: "revoked_jwts",
            column: "TokenHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "revoked_jwts");
    }
}
