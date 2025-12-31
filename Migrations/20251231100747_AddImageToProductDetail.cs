using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _204Store.Migrations
{
    /// <inheritdoc />
    public partial class AddImageToProductDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductDetails");
        }
    }
}
