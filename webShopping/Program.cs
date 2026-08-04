using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Globalization;
using webShopping.Data;
using webShopping.Email;
using webShopping.Models;
using webShopping.Services;

namespace webShopping
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Shared hosting (MonsterASP): never reject by Host header.
            builder.Configuration["AllowedHosts"] = "*";
            builder.Services.Configure<Microsoft.AspNetCore.HostFiltering.HostFilteringOptions>(options =>
            {
                options.AllowEmptyHosts = true;
                options.AllowedHosts.Clear();
                options.AllowedHosts.Add("*");
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Shared hosts recycle app pools often — persist antiforgery/identity keys to avoid HTTP 400.
            var dpKeys = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "dp-keys");
            Directory.CreateDirectory(dpKeys);
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dpKeys))
                .SetApplicationName("SagerShop");

            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "SagerShop.AF";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.ValueCountLimit = 4096;
                options.ValueLengthLimit = 4 * 1024 * 1024;
                options.KeyLengthLimit = 4096;
                options.MultipartBodyLengthLimit = 32 * 1024 * 1024;
            });

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient("ExchangeRates", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SagerShop/1.0");
            });
            builder.Services.AddSingleton<IExchangeRateService, ExchangeRateService>();
            builder.Services.AddHostedService<ExchangeRateRefreshHostedService>();
            builder.Services.AddScoped<ICurrencyService, CurrencyService>();
            builder.Services.AddScoped<IIyzipayCheckoutService, IyzipayCheckoutService>();
            builder.Services.AddScoped<IPayPalCheckoutService, PayPalCheckoutService>();
            builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
            builder.Services.AddScoped<IShippingService, ShippingService>();
            builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ISmsService, SmsService>();
            builder.Services.AddSingleton<IAppLocalizer, AppLocalizer>();
            builder.Services.AddSingleton<ISiteContentService, SiteContentService>();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
              .AddDefaultTokenProviders()
              .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddSingleton<IEmailSender, EmailSender>();

            var mvcBuilder = builder.Services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();

            if (builder.Environment.IsDevelopment())
                mvcBuilder.AddRazorRuntimeCompilation();

            builder.Services.AddRazorPages();

            builder.Services.ConfigureApplicationCookie(option =>
            {
                option.LoginPath = $"/Identity/Account/Login";
                option.LogoutPath = $"/Identity/Account/Logout";
                option.AccessDeniedPath = $"/Identity/Account/AccessDenied";
                option.Cookie.HttpOnly = true;
                option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                option.Cookie.SameSite = SameSiteMode.Lax;
                option.SlidingExpiration = true;
                option.ExpireTimeSpan = TimeSpan.FromDays(14);
            });

            var authBuilder = builder.Services.AddAuthentication();

            // Prefer env vars / web.config (Authentication__*) then appsettings.
            var fbAppId = builder.Configuration["Authentication:Facebook:AppId"];
            var fbSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];

            if (!string.IsNullOrWhiteSpace(fbAppId) && !string.IsNullOrWhiteSpace(fbSecret))
            {
                authBuilder.AddFacebook(option =>
                {
                    option.AppId = fbAppId.Trim();
                    option.AppSecret = fbSecret.Trim();
                    option.AccessDeniedPath = "/Identity/Account/AccessDenied";
                });
            }

            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleSecret))
            {
                authBuilder.AddGoogle(option =>
                {
                    option.ClientId = googleClientId.Trim();
                    option.ClientSecret = googleSecret.Trim();
                    option.AccessDeniedPath = "/Identity/Account/AccessDenied";
                });
            }

            builder.Services.AddMvc().AddNToastNotifyToastr(new ToastrOptions()
            {
                CloseButton = true,
                PositionClass = ToastPositions.TopRight,
                PreventDuplicates = true,
            });

            var app = builder.Build();

            // Never crash the app pool on startup failures (shared hosting).
            try
            {
                using var scope = app.Services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    if (db.Database.CanConnect())
                    {
                        // Self-heal first so existing hosting DBs stay compatible,
                        // then run EF migrations (idempotent for already-created tables).
                        try { EnsureDatabaseSchema(db); }
                        catch (Exception schemaEx)
                        {
                            logger.LogWarning(schemaEx, "Schema self-heal SQL failed.");
                        }

                        try { db.Database.Migrate(); }
                        catch (Exception migEx)
                        {
                            logger.LogWarning(migEx, "EF Migrate failed; schema self-heal already attempted.");
                            try { EnsureDatabaseSchema(db); }
                            catch (Exception schemaEx2)
                            {
                                logger.LogWarning(schemaEx2, "Schema self-heal retry failed.");
                            }
                        }

                        DbInitializer.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
                        scope.ServiceProvider.GetRequiredService<ISiteContentService>().Refresh();
                    }
                    else
                    {
                        logger.LogError("Cannot connect to SQL Server. Check ConnectionStrings:DefaultConnection.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Database migrate/seed failed during startup.");
                    WriteStartupError(app.Environment.ContentRootPath, ex);
                }

                try
                {
                    scope.ServiceProvider.GetRequiredService<IExchangeRateService>().RefreshAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Exchange rate refresh failed during startup (non-fatal).");
                }
            }
            catch (Exception ex)
            {
                WriteStartupError(app.Environment.ContentRootPath, ex);
            }

            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ar"),
                new CultureInfo("tr")
            };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("en")
                .AddSupportedCultures(supportedCultures.Select(c => c.Name).ToArray())
                .AddSupportedUICultures(supportedCultures.Select(c => c.Name).ToArray());
            localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
            app.UseRequestLocalization(localizationOptions);

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var exFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                        var ex = exFeature?.Error;
                        var path = exFeature?.Path ?? context.Request.Path.Value ?? "/";
                        var requestId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

                        if (ex != null)
                        {
                            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                                .CreateLogger("UnhandledException");
                            logger.LogError(ex, "Unhandled exception at {Path}", path);

                            try
                            {
                                RecentErrors.Store(requestId, new ErrorViewModel
                                {
                                    RequestId = requestId,
                                    Path = path,
                                    ExceptionType = ex.GetType().FullName,
                                    ExceptionMessage = ex.ToString(),
                                    StackTrace = ex.StackTrace
                                });
                            }
                            catch { /* ignore */ }

                            try
                            {
                                var logDir = Path.Combine(app.Environment.ContentRootPath, "logs");
                                Directory.CreateDirectory(logDir);
                                var line = $"{DateTime.UtcNow:u} | {path} | {requestId} | {ex}{Environment.NewLine}---{Environment.NewLine}";
                                await File.AppendAllTextAsync(Path.Combine(logDir, "errors.log"), line);
                            }
                            catch { /* ignore logging failures */ }
                        }

                        context.Response.Redirect($"/Home/Error?rid={Uri.EscapeDataString(requestId)}");
                    });
                });
                app.UseHsts();
            }

            app.UseForwardedHeaders();

            // Free hosts (runasp/IIS) often break when HTTPS binding hostname is missing.
            // Let the hosting panel handle SSL redirect; keep app-level redirect in Development only.
            if (app.Environment.IsDevelopment())
                app.UseHttpsRedirection();

            app.UseResponseCompression();
            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
                headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
                headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=(), payment=()");
                headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
                headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");

                // Shared host: HSTS only on HTTPS; IIS may already set Strict-Transport-Security.
                if (context.Request.IsHttps)
                {
                    headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                }

                // Allow Bootstrap/CDN assets used by the layout; 'unsafe-inline' for razor inline styles/scripts.
                const string csp =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdn.datatables.net https://cdnjs.cloudflare.com; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdn.datatables.net; " +
                    "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com data:; " +
                    "img-src 'self' data: blob: https:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'self'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'; " +
                    "object-src 'none'";
                headers.TryAdd("Content-Security-Policy", csp);

                await next();
            });

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }

        private static void EnsureDatabaseSchema(ApplicationDbContext db)
        {
            // Run statements separately: SQL Server validates column names at batch compile time,
            // so ADD + UPDATE of a new column in one batch fails before the IF is evaluated.
            void Exec(string sql)
            {
                try { db.Database.ExecuteSqlRaw(sql); }
                catch
                {
                    // Best-effort self-heal; individual steps may already exist.
                }
            }

            Exec("""
                IF COL_LENGTH('OrderHeaders', 'PaymentBankName') IS NULL
                    ALTER TABLE [OrderHeaders] ADD [PaymentBankName] nvarchar(max) NULL;
                """);
            Exec("""
                IF COL_LENGTH('OrderHeaders', 'PaymentReference') IS NULL
                    ALTER TABLE [OrderHeaders] ADD [PaymentReference] nvarchar(max) NULL;
                """);
            Exec("""
                IF COL_LENGTH('OrderHeaders', 'PaymentSubmittedAt') IS NULL
                    ALTER TABLE [OrderHeaders] ADD [PaymentSubmittedAt] datetime2 NULL;
                """);
            Exec("""
                IF COL_LENGTH('OrderHeaders', 'PaymentMethod') IS NULL
                    ALTER TABLE [OrderHeaders] ADD [PaymentMethod] nvarchar(max) NOT NULL CONSTRAINT DF_OrderHeaders_PaymentMethod DEFAULT('BankTransfer');
                """);
            Exec("""
                IF COL_LENGTH('OrderHeaders', 'TrackingNumber') IS NULL
                    ALTER TABLE [OrderHeaders] ADD [TrackingNumber] nvarchar(max) NULL;
                """);

            Exec("""
                IF COL_LENGTH('SiteSettings', 'BankTransferInfoEn') IS NULL
                    ALTER TABLE [SiteSettings] ADD [BankTransferInfoEn] nvarchar(max) NOT NULL CONSTRAINT DF_SiteSettings_BankEn DEFAULT('');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'BankTransferInfoAr') IS NULL
                    ALTER TABLE [SiteSettings] ADD [BankTransferInfoAr] nvarchar(max) NOT NULL CONSTRAINT DF_SiteSettings_BankAr DEFAULT('');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'SiteNameEn') IS NULL
                    ALTER TABLE [SiteSettings] ADD [SiteNameEn] nvarchar(120) NOT NULL CONSTRAINT DF_SiteSettings_SiteNameEn DEFAULT('Sager');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'SiteNameHighlightEn') IS NULL
                    ALTER TABLE [SiteSettings] ADD [SiteNameHighlightEn] nvarchar(120) NOT NULL CONSTRAINT DF_SiteSettings_SiteNameHiEn DEFAULT('Shop');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'SiteNameAr') IS NULL
                    ALTER TABLE [SiteSettings] ADD [SiteNameAr] nvarchar(120) NOT NULL CONSTRAINT DF_SiteSettings_SiteNameAr DEFAULT(N'ساجر');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'SiteNameHighlightAr') IS NULL
                    ALTER TABLE [SiteSettings] ADD [SiteNameHighlightAr] nvarchar(120) NOT NULL CONSTRAINT DF_SiteSettings_SiteNameHiAr DEFAULT(N'شوب');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'LogoPath') IS NULL
                    ALTER TABLE [SiteSettings] ADD [LogoPath] nvarchar(max) NOT NULL CONSTRAINT DF_SiteSettings_LogoPath DEFAULT('');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'ThemePrimary') IS NULL
                    ALTER TABLE [SiteSettings] ADD [ThemePrimary] nvarchar(20) NOT NULL CONSTRAINT DF_SiteSettings_ThemePrimary DEFAULT('#151528');
                """);
            Exec("""
                IF COL_LENGTH('SiteSettings', 'ThemeAccent') IS NULL
                    ALTER TABLE [SiteSettings] ADD [ThemeAccent] nvarchar(20) NOT NULL CONSTRAINT DF_SiteSettings_ThemeAccent DEFAULT('#e23b58');
                """);

            Exec("""
                IF COL_LENGTH('Categoties', 'ImagePath') IS NULL
                    ALTER TABLE [Categoties] ADD [ImagePath] nvarchar(max) NOT NULL CONSTRAINT DF_Categoties_ImagePath DEFAULT('');
                """);

            Exec("""
                IF COL_LENGTH('Products', 'StockQuantity') IS NULL
                    ALTER TABLE [Products] ADD [StockQuantity] int NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT(0);
                """);
            Exec("""
                IF COL_LENGTH('Products', 'StockQuantity') IS NOT NULL
                    UPDATE [Products]
                    SET [StockQuantity] = 10
                    WHERE [IsStock] = 1 AND [StockQuantity] = 0;
                """);

            Exec("""
                IF OBJECT_ID(N'[AppNotifications]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AppNotifications](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [UserId] [nvarchar](450) NOT NULL,
                        [TitleEn] [nvarchar](max) NOT NULL,
                        [TitleAr] [nvarchar](max) NOT NULL,
                        [MessageEn] [nvarchar](max) NOT NULL,
                        [MessageAr] [nvarchar](max) NOT NULL,
                        [LinkUrl] [nvarchar](max) NULL,
                        [Type] [nvarchar](max) NOT NULL,
                        [OrderId] [int] NULL,
                        [IsRead] [bit] NOT NULL,
                        [CreatedAt] [datetime2] NOT NULL,
                        CONSTRAINT [PK_AppNotifications] PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT [FK_AppNotifications_AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_AppNotifications_UserId_IsRead] ON [AppNotifications]([UserId], [IsRead]);
                    CREATE INDEX [IX_AppNotifications_CreatedAt] ON [AppNotifications]([CreatedAt]);
                END
                """);

            Exec("""
                IF OBJECT_ID(N'[BankAccounts]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BankAccounts](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [BankNameEn] [nvarchar](120) NOT NULL,
                        [BankNameAr] [nvarchar](120) NOT NULL,
                        [AccountNumber] [nvarchar](80) NOT NULL,
                        [Iban] [nvarchar](80) NULL,
                        [BeneficiaryEn] [nvarchar](120) NOT NULL,
                        [BeneficiaryAr] [nvarchar](120) NOT NULL,
                        [NotesEn] [nvarchar](300) NULL,
                        [NotesAr] [nvarchar](300) NULL,
                        [SortOrder] [int] NOT NULL,
                        [IsActive] [bit] NOT NULL,
                        CONSTRAINT [PK_BankAccounts] PRIMARY KEY CLUSTERED ([Id] ASC)
                    );
                END
                """);

            Exec("""
                IF COL_LENGTH('Products', 'SalePrice') IS NULL
                    ALTER TABLE [Products] ADD [SalePrice] float NULL;
                """);
            Exec("""
                IF COL_LENGTH('Products', 'Colors') IS NULL
                    ALTER TABLE [Products] ADD [Colors] nvarchar(500) NOT NULL CONSTRAINT DF_Products_Colors DEFAULT('');
                """);
            Exec("""
                IF COL_LENGTH('Products', 'Sizes') IS NULL
                    ALTER TABLE [Products] ADD [Sizes] nvarchar(500) NOT NULL CONSTRAINT DF_Products_Sizes DEFAULT('');
                """);
            Exec("""
                IF COL_LENGTH('Products', 'BrandId') IS NULL
                    ALTER TABLE [Products] ADD [BrandId] int NULL;
                """);
            Exec("""
                IF COL_LENGTH('Categoties', 'SortOrder') IS NULL
                    ALTER TABLE [Categoties] ADD [SortOrder] int NOT NULL CONSTRAINT DF_Categoties_SortOrder DEFAULT(0);
                """);
            Exec("""
                IF COL_LENGTH('OrderHeaders', 'DiscountUsd') IS NULL
                    ALTER TABLE [OrderHeaders] ADD [DiscountUsd] float NOT NULL CONSTRAINT DF_OrderHeaders_DiscountUsd DEFAULT(0);
                """);
            Exec("""
                IF COL_LENGTH('OrderHeaders', 'CouponCode') IS NULL
                    ALTER TABLE [OrderHeaders] ADD [CouponCode] nvarchar(40) NULL;
                """);

            Exec("""
                IF OBJECT_ID(N'[Brands]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Brands](
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Name] nvarchar(120) NOT NULL,
                        [NameAr] nvarchar(120) NOT NULL,
                        [ImagePath] nvarchar(max) NOT NULL,
                        [SortOrder] int NOT NULL,
                        [IsActive] bit NOT NULL
                    );
                END
                """);
            Exec("""
                IF OBJECT_ID(N'[Banners]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Banners](
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [TitleEn] nvarchar(160) NOT NULL,
                        [TitleAr] nvarchar(160) NOT NULL,
                        [SubtitleEn] nvarchar(300) NOT NULL,
                        [SubtitleAr] nvarchar(300) NOT NULL,
                        [ImagePath] nvarchar(max) NOT NULL,
                        [LinkUrl] nvarchar(max) NOT NULL,
                        [SortOrder] int NOT NULL,
                        [IsActive] bit NOT NULL
                    );
                END
                """);
            Exec("""
                IF OBJECT_ID(N'[Coupons]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Coupons](
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Code] nvarchar(40) NOT NULL,
                        [TitleEn] nvarchar(120) NOT NULL,
                        [TitleAr] nvarchar(120) NOT NULL,
                        [Value] float NOT NULL,
                        [IsPercent] bit NOT NULL,
                        [MinOrderUsd] float NOT NULL,
                        [MaxUses] int NOT NULL,
                        [UsedCount] int NOT NULL,
                        [StartsAt] datetime2 NULL,
                        [EndsAt] datetime2 NULL,
                        [IsActive] bit NOT NULL
                    );
                END
                """);
            Exec("""
                IF OBJECT_ID(N'[NewsletterSubscribers]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [NewsletterSubscribers](
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Email] nvarchar(200) NOT NULL,
                        [SubscribedAt] datetime2 NOT NULL,
                        [IsActive] bit NOT NULL
                    );
                END
                """);
            Exec("""
                IF OBJECT_ID(N'[ContactMessages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ContactMessages](
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Name] nvarchar(120) NOT NULL,
                        [Email] nvarchar(200) NOT NULL,
                        [Subject] nvarchar(200) NOT NULL,
                        [Message] nvarchar(2000) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [IsRead] bit NOT NULL,
                        [AdminReply] nvarchar(max) NULL,
                        [RepliedAt] datetime2 NULL
                    );
                END
                """);
            Exec("""
                IF OBJECT_ID(N'[FaqItems]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [FaqItems](
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [QuestionEn] nvarchar(300) NOT NULL,
                        [QuestionAr] nvarchar(300) NOT NULL,
                        [AnswerEn] nvarchar(max) NOT NULL,
                        [AnswerAr] nvarchar(max) NOT NULL,
                        [SortOrder] int NOT NULL,
                        [IsActive] bit NOT NULL
                    );
                END
                """);

            Exec("""
                IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NOT NULL
                   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory])
                BEGIN
                    IF OBJECT_ID(N'[AppNotifications]', N'U') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260710140000_AppNotifications')
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES (N'20260710140000_AppNotifications', N'8.0.4');

                    IF OBJECT_ID(N'[BankAccounts]', N'U') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260804115711_511nm')
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES (N'20260804115711_511nm', N'8.0.4');
                END
                """);
        }

        private static void WriteStartupError(string contentRoot, Exception ex)
        {
            try
            {
                var logDir = Path.Combine(contentRoot, "logs");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(
                    Path.Combine(logDir, "startup-errors.log"),
                    $"{DateTime.UtcNow:u}{Environment.NewLine}{ex}{Environment.NewLine}---{Environment.NewLine}");
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}
