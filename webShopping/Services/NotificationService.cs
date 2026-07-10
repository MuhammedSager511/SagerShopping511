using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Services
{
    public interface INotificationService
    {
        Task NotifyUserAsync(string userId, string titleKey, string messageKey,
            IReadOnlyDictionary<string, string>? replacements = null,
            string? linkUrl = null, string type = "info", int? orderId = null);

        Task NotifyAdminsAsync(string titleKey, string messageKey,
            IReadOnlyDictionary<string, string>? replacements = null,
            string? linkUrl = null, string type = "info", int? orderId = null);

        Task<int> GetUnreadCountAsync(string userId);
        Task<List<AppNotification>> GetRecentAsync(string userId, int take = 8);
        Task<List<AppNotification>> GetAllAsync(string userId, int page = 1, int pageSize = 30);
        Task MarkAsReadAsync(int id, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task<AppNotification?> GetAsync(int id, string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppLocalizer _localizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(
            ApplicationDbContext db,
            IAppLocalizer localizer,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _localizer = localizer;
            _userManager = userManager;
        }

        public async Task NotifyUserAsync(string userId, string titleKey, string messageKey,
            IReadOnlyDictionary<string, string>? replacements = null,
            string? linkUrl = null, string type = "info", int? orderId = null)
        {
            if (string.IsNullOrEmpty(userId)) return;
            await SaveAsync(userId, titleKey, messageKey, replacements, linkUrl, type, orderId);
        }

        public async Task NotifyAdminsAsync(string titleKey, string messageKey,
            IReadOnlyDictionary<string, string>? replacements = null,
            string? linkUrl = null, string type = "info", int? orderId = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync(Diger.Role_Admin);
            foreach (var admin in admins)
            {
                await SaveAsync(admin.Id, titleKey, messageKey, replacements, linkUrl, type, orderId);
            }
        }

        public Task<int> GetUnreadCountAsync(string userId) =>
            _db.AppNotifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public Task<List<AppNotification>> GetRecentAsync(string userId, int take = 8) =>
            _db.AppNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();

        public Task<List<AppNotification>> GetAllAsync(string userId, int page = 1, int pageSize = 30) =>
            _db.AppNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task MarkAsReadAsync(int id, string userId)
        {
            var notification = await _db.AppNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification == null) return;
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _db.AppNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public Task<AppNotification?> GetAsync(int id, string userId) =>
            _db.AppNotifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        private async Task SaveAsync(string userId, string titleKey, string messageKey,
            IReadOnlyDictionary<string, string>? replacements, string? linkUrl, string type, int? orderId)
        {
            var titleEn = Apply(_localizer.Get(titleKey, "en"), replacements);
            var titleAr = Apply(_localizer.Get(titleKey, "ar"), replacements);
            var messageEn = Apply(_localizer.Get(messageKey, "en"), replacements);
            var messageAr = Apply(_localizer.Get(messageKey, "ar"), replacements);

            _db.AppNotifications.Add(new AppNotification
            {
                UserId = userId,
                TitleEn = titleEn,
                TitleAr = titleAr,
                MessageEn = messageEn,
                MessageAr = messageAr,
                LinkUrl = linkUrl,
                Type = type,
                OrderId = orderId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        private static string Apply(string template, IReadOnlyDictionary<string, string>? replacements)
        {
            if (replacements == null) return template;
            foreach (var kv in replacements)
                template = template.Replace($"{{{kv.Key}}}", kv.Value);
            return template;
        }
    }
}
