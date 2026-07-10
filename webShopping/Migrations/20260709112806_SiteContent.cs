using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Migrations
{
    /// <inheritdoc />
    public partial class SiteContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "QuickLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeroTitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeroTitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeroSubtitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeroSubtitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AboutTitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AboutTitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AboutBodyEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AboutBodyAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FooterTextEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FooterTextAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactAddressEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactAddressAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactHoursEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactHoursAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FacebookUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstagramUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TwitterUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatsAppUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuickLinks");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");
        }
    }
}
