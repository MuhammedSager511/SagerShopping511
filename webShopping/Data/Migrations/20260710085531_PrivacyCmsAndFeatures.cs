using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrivacyCmsAndFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrivacyBodyAr",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrivacyBodyEn",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrivacyTitleAr",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrivacyTitleEn",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivacyBodyAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PrivacyBodyEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PrivacyTitleAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PrivacyTitleEn",
                table: "SiteSettings");
        }
    }
}
