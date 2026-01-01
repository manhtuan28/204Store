using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _204Store.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReturnAttempts",
                table: "OrderHeaders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnAttempts",
                table: "OrderHeaders");
        }
    }
}
