using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webShopping.Migrations
{
    public partial class AppNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[AppNotifications]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AppNotifications](
                        [Id] int NOT NULL IDENTITY(1,1),
                        [UserId] nvarchar(450) NOT NULL,
                        [TitleEn] nvarchar(max) NOT NULL,
                        [TitleAr] nvarchar(max) NOT NULL,
                        [MessageEn] nvarchar(max) NOT NULL,
                        [MessageAr] nvarchar(max) NOT NULL,
                        [LinkUrl] nvarchar(max) NULL,
                        [Type] nvarchar(max) NOT NULL,
                        [OrderId] int NULL,
                        [IsRead] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_AppNotifications] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_AppNotifications_AspNetUsers_UserId]
                            FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_AppNotifications_UserId_IsRead] ON [AppNotifications]([UserId], [IsRead]);
                    CREATE INDEX [IX_AppNotifications_CreatedAt] ON [AppNotifications]([CreatedAt]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[AppNotifications]', N'U') IS NOT NULL
                    DROP TABLE [AppNotifications];
                """);
        }
    }
}
