using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Data
{
    public static class DbInitializer
    {
        private static readonly Dictionary<string, string> CategoryAr = new()
        {
            ["Electronics"] = "إلكترونيات",
            ["Fashion"] = "أزياء",
            ["Home & Living"] = "المنزل والمعيشة",
            ["Sports"] = "رياضة"
        };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var db = serviceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roles = { Diger.Role_Admin, Diger.Role_User, Diger.Role_Birey };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            if (!db.SiteSettings.Any())
            {
                db.SiteSettings.Add(new SiteSettings());
                await db.SaveChangesAsync();
            }
            else
            {
                var settings = await db.SiteSettings.FirstAsync();
                if (string.IsNullOrWhiteSpace(settings.PrivacyBodyEn))
                {
                    settings.PrivacyTitleEn = "Privacy Policy";
                    settings.PrivacyTitleAr = "سياسة الخصوصية";
                    settings.PrivacyBodyEn = "We respect your privacy. We collect only data needed to process orders. Card data is never stored on our servers.";
                    settings.PrivacyBodyAr = "نحترم خصوصيتك. نجمع فقط البيانات اللازمة لمعالجة الطلبات. لا نخزّن بيانات البطاقة على خوادمنا.";
                    await db.SaveChangesAsync();
                }
                if (string.IsNullOrWhiteSpace(settings.BankTransferInfoAr))
                {
                    settings.BankTransferInfoEn = new SiteSettings().BankTransferInfoEn;
                    settings.BankTransferInfoAr = new SiteSettings().BankTransferInfoAr;
                    await db.SaveChangesAsync();
                }
            }

            if (!db.QuickLinks.Any())
            {
                db.QuickLinks.AddRange(
                    new QuickLink { TitleEn = "All Products", TitleAr = "كل المنتجات", Url = "/Home/Shop", SortOrder = 1 },
                    new QuickLink { TitleEn = "Featured", TitleAr = "منتجات مميزة", Url = "/Home", SortOrder = 2 },
                    new QuickLink { TitleEn = "My Orders", TitleAr = "طلباتي", Url = "/Order", SortOrder = 3 },
                    new QuickLink { TitleEn = "Track Order", TitleAr = "تتبع الطلب", Url = "/Order/Track", SortOrder = 4 },
                    new QuickLink { TitleEn = "About Us", TitleAr = "من نحن", Url = "/Home/About", SortOrder = 5 }
                );
                await db.SaveChangesAsync();
            }

            if (!db.Categoties.Any())
            {
                db.Categoties.AddRange(
                    new Categoty { Name = "Electronics", NameAr = "إلكترونيات" },
                    new Categoty { Name = "Fashion", NameAr = "أزياء" },
                    new Categoty { Name = "Home & Living", NameAr = "المنزل والمعيشة" },
                    new Categoty { Name = "Sports", NameAr = "رياضة" }
                );
                await db.SaveChangesAsync();
            }
            else
            {
                foreach (var cat in db.Categoties.Where(c => string.IsNullOrEmpty(c.NameAr)))
                {
                    if (CategoryAr.TryGetValue(cat.Name, out var ar))
                        cat.NameAr = ar;
                }
                await db.SaveChangesAsync();
            }

            foreach (var product in db.Products.Where(p => string.IsNullOrEmpty(p.NameAr)))
                product.NameAr = product.Name;

            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync();

            if (!db.ShippingSettings.Any())
            {
                db.ShippingSettings.Add(new ShippingSettings
                {
                    StoreCountry = "Syria",
                    StoreCity = "Damascus",
                    FreeShippingSameCity = true,
                    DefaultCostUsd = 18m,
                    DefaultMinDays = 7,
                    DefaultMaxDays = 21
                });
                await db.SaveChangesAsync();
            }

            if (!db.ShippingZones.Any())
            {
                db.ShippingZones.AddRange(
                    new ShippingZone { Country = "Syria", City = "Damascus", ShippingCostUsd = 0, IsFreeShipping = true, DeliveryMinDays = 2, DeliveryMaxDays = 5, SortOrder = 1 },
                    new ShippingZone { Country = "Syria", City = "Aleppo", ShippingCostUsd = 2, VatRate = 0, DeliveryMinDays = 3, DeliveryMaxDays = 7, SortOrder = 2 },
                    new ShippingZone { Country = "Syria", City = "حلب", ShippingCostUsd = 2, DeliveryMinDays = 3, DeliveryMaxDays = 7, SortOrder = 3 },
                    new ShippingZone { Country = "Syria", ShippingCostUsd = 12, VatRate = 0, DeliveryMinDays = 7, DeliveryMaxDays = 14, SortOrder = 4 },
                    new ShippingZone { Country = "Turkey", City = "Istanbul", ShippingCostUsd = 0, DeliveryMinDays = 2, DeliveryMaxDays = 5, SortOrder = 10 },
                    new ShippingZone { Country = "Turkey", ShippingCostUsd = 5, VatRate = 0.20m, DeliveryMinDays = 2, DeliveryMaxDays = 5, SortOrder = 11 },
                    new ShippingZone { Country = "United States", ShippingCostUsd = 15, VatRate = 0.08m, DeliveryMinDays = 5, DeliveryMaxDays = 12, SortOrder = 20 },
                    new ShippingZone { Country = "United Kingdom", ShippingCostUsd = 10, VatRate = 0.20m, DeliveryMinDays = 4, DeliveryMaxDays = 8, SortOrder = 21 },
                    new ShippingZone { Country = "Germany", ShippingCostUsd = 9, VatRate = 0.19m, DeliveryMinDays = 3, DeliveryMaxDays = 7, SortOrder = 22 }
                );
                await db.SaveChangesAsync();
            }

            var email = configuration["AdminSeed:Email"] ?? "admin@webshopping.com";
            var password = configuration["AdminSeed:Password"];
            var name = configuration["AdminSeed:Name"] ?? "Admin";
            var lastName = configuration["AdminSeed:LastName"] ?? "User";

            if (string.IsNullOrWhiteSpace(password))
            {
                var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("DbInitializer");
                logger?.LogWarning(
                    "AdminSeed:Password is not set. Skipping admin user creation. " +
                    "Set via User Secrets (dev) or environment variables (production).");
            }
            else
            {
                var adminUser = await userManager.FindByEmailAsync(email);
                if (adminUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        Name = name,
                        LastName = lastName,
                        PhoneNumberConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, Diger.Role_Admin);
                }
                else if (!await userManager.IsInRoleAsync(adminUser, Diger.Role_Admin))
                {
                    await userManager.AddToRoleAsync(adminUser, Diger.Role_Admin);
                }

                if (configuration.GetValue<bool>("AdminSeed:ResetPassword"))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var reset = await userManager.ResetPasswordAsync(adminUser, token, password);
                    if (!reset.Succeeded)
                    {
                        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("DbInitializer");
                        logger?.LogWarning("Admin password reset failed: {Errors}",
                            string.Join(", ", reset.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
    }
}
