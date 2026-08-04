using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
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
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext db,
            IAppLocalizer localizer,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationService> logger)
        {
            _db = db;
            _localizer = localizer;
            _userManager = userManager;
            _logger = logger;
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

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                return await _db.AppNotifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception ex) when (IsMissingNotificationTable(ex))
            {
                _logger.LogWarning("AppNotifications table is missing. Returning unread count = 0.");
                return 0;
            }
        }

        public async Task<List<AppNotification>> GetRecentAsync(string userId, int take = 8)
        {
            try
            {
                return await _db.AppNotifications
                    .AsNoTracking()
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex) when (IsMissingNotificationTable(ex))
            {
                _logger.LogWarning("AppNotifications table is missing. Returning empty recent notifications.");
                return [];
            }
        }

        public async Task<List<AppNotification>> GetAllAsync(string userId, int page = 1, int pageSize = 30)
        {
            try
            {
                return await _db.AppNotifications
                    .AsNoTracking()
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex) when (IsMissingNotificationTable(ex))
            {
                _logger.LogWarning("AppNotifications table is missing. Returning empty notification list.");
                return [];
            }
        }

        public async Task MarkAsReadAsync(int id, string userId)
        {
            try
            {
                var notification = await _db.AppNotifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
                if (notification == null) return;
                notification.IsRead = true;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) when (IsMissingNotificationTable(ex))
            {
                _logger.LogWarning("AppNotifications table is missing. Skipping MarkAsRead.");
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            try
            {
                await _db.AppNotifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            }
            catch (Exception ex) when (IsMissingNotificationTable(ex))
            {
                _logger.LogWarning("AppNotifications table is missing. Skipping MarkAllAsRead.");
            }
        }

        public async Task<AppNotification?> GetAsync(int id, string userId)
        {
            try
            {
                return await _db.AppNotifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            }
            catch (Exception ex) when (IsMissingNotificationTable(ex))
            {
                _logger.LogWarning("AppNotifications table is missing. Returning null from GetAsync.");
                return null;
            }
        }

        private async Task SaveAsync(string userId, string titleKey, string messageKey,
            IReadOnlyDictionary<string, string>? replacements, string? linkUrl, string type, int? orderId)
        {
            var titleEn = Apply(_localizer.Get(titleKey, "en"), replacements);
            var titleAr = Apply(_localizer.Get(titleKey, "ar"), replacements);
            var messageEn = Apply(_localizer.Get(messageKey, "en"), replacements);
            var messageAr = Apply(_localizer.Get(messageKey, "ar"), replacements);

            try
            {
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
            catch (Exception ex) when (IsMissingNotificationTable(ex))
            {
                _logger.LogWarning("AppNotifications table is missing. Skipping SaveAsync.");
            }
        }

        private static string Apply(string template, IReadOnlyDictionary<string, string>? replacements)
        {
            if (replacements == null) return template;
            foreach (var kv in replacements)
                template = template.Replace($"{{{kv.Key}}}", kv.Value);
            return template;
        }

        private static bool IsMissingNotificationTable(Exception ex)
        {
            if (ex is SqlException sql && sql.Message.Contains("AppNotifications", StringComparison.OrdinalIgnoreCase))
                return true;

            var inner = ex.InnerException;
            return inner is SqlException innerSql &&
                   innerSql.Message.Contains("AppNotifications", StringComparison.OrdinalIgnoreCase);
        }
    }
}
