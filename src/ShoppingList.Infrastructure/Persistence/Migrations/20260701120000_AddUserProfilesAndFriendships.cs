using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUserProfilesAndFriendships : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "Friend");

        migrationBuilder.AddColumn<string>(
            name: "FriendCode",
            table: "users",
            type: "character varying(8)",
            maxLength: 8,
            nullable: false,
            defaultValue: "TEMP0000");

        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PhoneNumber",
            table: "users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE users
            SET "FirstName" = COALESCE(
                NULLIF(split_part(COALESCE("DisplayName", ''), ' ', 1), ''),
                'Friend')
            WHERE "FirstName" = 'Friend';
            """);

        migrationBuilder.Sql("""
            UPDATE users
            SET "FriendCode" = upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))
            WHERE "FriendCode" = 'TEMP0000';
            """);

        migrationBuilder.Sql("""
            UPDATE users
            SET "FirstName" = 'Sarah',
                "DisplayName" = 'Sarah',
                "PhoneNumber" = '+15551234567'
            WHERE "Id" = '22222222-2222-2222-2222-222222222222';
            """);

        migrationBuilder.Sql("""
            INSERT INTO users ("Id", "Email", "PasswordHash", "DisplayName", "FirstName", "LastName", "PhoneNumber", "FriendCode", "CreatedAt")
            SELECT '55555555-5555-5555-5555-555555555555', 'alex@shoppinglist.local', 'dev-only', 'Alex Morgan', 'Alex', 'Morgan', '+15559876543', upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 8)), NOW()
            WHERE NOT EXISTS (
                SELECT 1 FROM users WHERE "Id" = '55555555-5555-5555-5555-555555555555');
            """);

        migrationBuilder.AlterColumn<string>(
            name: "FriendCode",
            table: "users",
            type: "character varying(8)",
            maxLength: 8,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(8)",
            oldMaxLength: 8,
            oldDefaultValue: "TEMP0000");

        migrationBuilder.CreateIndex(
            name: "IX_users_FriendCode",
            table: "users",
            column: "FriendCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_PhoneNumber",
            table: "users",
            column: "PhoneNumber",
            unique: true,
            filter: "\"PhoneNumber\" IS NOT NULL");

        migrationBuilder.CreateTable(
            name: "friendships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                AddresseeId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_friendships", x => x.Id);
                table.ForeignKey(
                    name: "FK_friendships_users_AddresseeId",
                    column: x => x.AddresseeId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_friendships_users_RequesterId",
                    column: x => x.RequesterId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_friendships_AddresseeId",
            table: "friendships",
            column: "AddresseeId");

        migrationBuilder.CreateIndex(
            name: "IX_friendships_RequesterId_AddresseeId",
            table: "friendships",
            columns: new[] { "RequesterId", "AddresseeId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_friendships_Status",
            table: "friendships",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "friendships");

        migrationBuilder.DropIndex(
            name: "IX_users_FriendCode",
            table: "users");

        migrationBuilder.DropIndex(
            name: "IX_users_PhoneNumber",
            table: "users");

        migrationBuilder.DropColumn(
            name: "FirstName",
            table: "users");

        migrationBuilder.DropColumn(
            name: "FriendCode",
            table: "users");

        migrationBuilder.DropColumn(
            name: "LastName",
            table: "users");

        migrationBuilder.DropColumn(
            name: "PhoneNumber",
            table: "users");
    }
}
