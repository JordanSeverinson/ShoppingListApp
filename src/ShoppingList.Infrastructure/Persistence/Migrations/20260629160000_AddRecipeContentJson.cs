using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecipeContentJson : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Content",
            table: "recipes",
            type: "jsonb",
            nullable: false,
            defaultValue: "{\"recipe\":{\"cookingSteps\":[]}}");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Content",
            table: "recipes");
    }
}
