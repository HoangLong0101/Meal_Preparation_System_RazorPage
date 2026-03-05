using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPrepService.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishedToMealRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Finished",
                table: "MealRecipes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Finished",
                table: "MealRecipes");
        }
    }
}
