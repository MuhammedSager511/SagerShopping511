using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Migrations
{
    /// <inheritdoc />
    public partial class _511nm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Table may already exist from Program.EnsureDatabaseSchema on shared hosting.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[BankAccounts]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BankAccounts](
                        [Id] int NOT NULL IDENTITY(1,1),
                        [BankNameEn] nvarchar(120) NOT NULL,
                        [BankNameAr] nvarchar(120) NOT NULL,
                        [AccountNumber] nvarchar(80) NOT NULL,
                        [Iban] nvarchar(80) NULL,
                        [BeneficiaryEn] nvarchar(120) NOT NULL,
                        [BeneficiaryAr] nvarchar(120) NOT NULL,
                        [NotesEn] nvarchar(300) NULL,
                        [NotesAr] nvarchar(300) NULL,
                        [SortOrder] int NOT NULL,
                        [IsActive] bit NOT NULL,
                        CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id])
                    );
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[BankAccounts]', N'U') IS NOT NULL
                    DROP TABLE [BankAccounts];
                """);
        }
    }
}
