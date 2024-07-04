using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Migrations
{
    /// <inheritdoc />
    public partial class productvm1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_OrderHeaders_orderHeaderId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_orderHeaderId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "orderHeaderId",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "orderHeaderId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_orderHeaderId",
                table: "Products",
                column: "orderHeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_OrderHeaders_orderHeaderId",
                table: "Products",
                column: "orderHeaderId",
                principalTable: "OrderHeaders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
