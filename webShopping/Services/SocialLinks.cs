using System.Text.RegularExpressions;

namespace webShopping.Services
{
    public static class SocialLinks
    {
        /// <summary>
        /// Builds a working WhatsApp chat URL from a phone number or full wa.me / api link.
        /// </summary>
        public static string WhatsApp(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "#")
                return "#";

            var input = value.Trim();

            // Already an absolute URL
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("whatsapp://", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            // Digits only (keep leading country code; strip spaces/dashes/+ etc except digits)
            var digits = Regex.Replace(input, @"\D", "");
            if (digits.Length >= 8)
                return $"https://wa.me/{digits}";

            // Relative path like "wa.me/..." without scheme
            if (input.Contains("wa.me", StringComparison.OrdinalIgnoreCase) ||
                input.Contains("whatsapp", StringComparison.OrdinalIgnoreCase))
                return "https://" + input.TrimStart('/');

            return input.StartsWith('/') ? input : "https://" + input;
        }

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "#")
                return "#";

            var input = value.Trim();
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith('/'))
                return input;

            return "https://" + input.TrimStart('/');
        }
    }
}
