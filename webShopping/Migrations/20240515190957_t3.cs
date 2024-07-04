using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Migrations
{
    /// <inheritdoc />
    public partial class t3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orderDetailses_OrederId",
                table: "orderDetailses");

            migrationBuilder.CreateIndex(
                name: "IX_orderDetailses_OrederId",
                table: "orderDetailses",
                column: "OrederId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orderDetailses_OrederId",
                table: "orderDetailses");

            migrationBuilder.CreateIndex(
                name: "IX_orderDetailses_OrederId",
                table: "orderDetailses",
                column: "OrederId",
                unique: true);
        }
    }
}
