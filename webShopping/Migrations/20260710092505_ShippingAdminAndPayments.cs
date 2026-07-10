using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Migrations
{
    /// <inheritdoc />
    public partial class ShippingAdminAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayPalOrderId",
                table: "OrderHeaders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "OrderHeaders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ShippingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreCountry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreCity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FreeShippingSameCity = table.Column<bool>(type: "bit", nullable: false),
                    FreeShippingMinOrderUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultCostUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultVatRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultMinDays = table.Column<int>(type: "int", nullable: false),
                    DefaultMaxDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShippingCostUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveryMinDays = table.Column<int>(type: "int", nullable: false),
                    DeliveryMaxDays = table.Column<int>(type: "int", nullable: false),
                    IsFreeShipping = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingZones", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingSettings");

            migrationBuilder.DropTable(
                name: "ShippingZones");

            migrationBuilder.DropColumn(
                name: "PayPalOrderId",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "OrderHeaders");
        }
    }
}
