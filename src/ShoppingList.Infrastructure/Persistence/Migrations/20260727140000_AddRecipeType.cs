using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecipeType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RecipeType",
            table: "recipes",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_recipes_RecipeType",
            table: "recipes",
            column: "RecipeType");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_recipes_RecipeType",
            table: "recipes");

        migrationBuilder.DropColumn(
            name: "RecipeType",
            table: "recipes");
    }
}
