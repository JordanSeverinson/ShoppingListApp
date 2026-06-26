using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <summary>
/// Restores the EF model snapshot only. Schema is created by <see cref="InitialCreate"/>.
/// </summary>
public partial class SyncModelSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty — InitialCreate owns the schema.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty.
    }
}
