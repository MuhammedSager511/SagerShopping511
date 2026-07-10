using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddDefaultTokenProviders()
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
            });

            var authBuilder = builder.Services.AddAuthentication();

            var fbAppId = builder.Configuration["Authentication:Facebook:AppId"];
            var fbSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
            if (!string.IsNullOrWhiteSpace(fbAppId) && !string.IsNullOrWhiteSpace(fbSecret))
            {
                authBuilder.AddFacebook(option =>
                {
                    option.AppId = fbAppId;
                    option.AppSecret = fbSecret;
                });
            }

            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleSecret))
            {
                authBuilder.AddGoogle(option =>
                {
                    option.ClientId = googleClientId;
                    option.ClientSecret = googleSecret;
                });
            }

            builder.Services.AddMvc().AddNToastNotifyToastr(new ToastrOptions()
            {
                CloseButton = true,
                PositionClass = ToastPositions.TopRight,
                PreventDuplicates = true,
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                DbInitializer.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
                scope.ServiceProvider.GetRequiredService<ISiteContentService>().Refresh();
                scope.ServiceProvider.GetRequiredService<IExchangeRateService>().RefreshAsync().GetAwaiter().GetResult();
            }

            var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("en")
                .AddSupportedCultures(supportedCultures.Select(c => c.Name).ToArray())
                .AddSupportedUICultures(supportedCultures.Select(c => c.Name).ToArray());
            localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
            app.UseRequestLocalization(localizationOptions);

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
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
    }
}
