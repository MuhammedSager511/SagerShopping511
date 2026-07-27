using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Migrations
{
    /// <inheritdoc />
    public partial class AddBankTransferSiteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankTransferInfoAr",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankTransferInfoEn",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankTransferInfoAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BankTransferInfoEn",
                table: "SiteSettings");
        }
    }
}
