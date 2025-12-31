using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _204Store.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specifications",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "Products");
        }
    }
}
