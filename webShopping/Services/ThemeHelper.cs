using System.Globalization;
using System.Text.RegularExpressions;
using webShopping.Models;

namespace webShopping.Services
{
    public static class ThemeHelper
    {
        private static readonly Regex HexRx = new(@"^#?([0-9A-Fa-f]{6})$", RegexOptions.Compiled);

        public static string NormalizeHex(string? hex, string fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            var m = HexRx.Match(hex.Trim());
            if (!m.Success) return fallback;
            return "#" + m.Groups[1].Value.ToUpperInvariant();
        }

        public static (int R, int G, int B) ParseRgb(string hex)
        {
            hex = NormalizeHex(hex, "#000000").TrimStart('#');
            return (
                int.Parse(hex[..2], NumberStyles.HexNumber),
                int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber)
            );
        }

        public static string Darken(string hex, double amount = 0.14)
        {
            var (r, g, b) = ParseRgb(hex);
            r = (int)Math.Clamp(r * (1 - amount), 0, 255);
            g = (int)Math.Clamp(g * (1 - amount), 0, 255);
            b = (int)Math.Clamp(b * (1 - amount), 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public static string Lighten(string hex, double amount = 0.2)
        {
            var (r, g, b) = ParseRgb(hex);
            r = (int)Math.Clamp(r + (255 - r) * amount, 0, 255);
            g = (int)Math.Clamp(g + (255 - g) * amount, 0, 255);
            b = (int)Math.Clamp(b + (255 - b) * amount, 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public static string ToRgba(string hex, double alpha)
        {
            var (r, g, b) = ParseRgb(hex);
            return $"rgba({r}, {g}, {b}, {alpha.ToString(CultureInfo.InvariantCulture)})";
        }

        public static string CssVariables(SiteSettings s)
        {
            var primary = NormalizeHex(s.ThemePrimary, "#151528");
            var accent = NormalizeHex(s.ThemeAccent, "#e23b58");
            var hover = Darken(accent, 0.14);
            var soft = ToRgba(accent, 0.12);
            var ring = ToRgba(accent, 0.28);
            var primarySoft = Lighten(primary, 0.08);
            return $"""
                --primary: {primary};
                --primary-soft: {primarySoft};
                --accent: {accent};
                --accent-hover: {hover};
                --accent-soft: {soft};
                --ring: 0 0 0 3px {ring};
                """;
        }

        public static string FullSiteName(SiteSettings s, bool arabic)
        {
            var a = arabic
                ? (string.IsNullOrWhiteSpace(s.SiteNameAr) ? s.SiteNameEn : s.SiteNameAr)
                : s.SiteNameEn;
            var b = arabic
                ? (string.IsNullOrWhiteSpace(s.SiteNameHighlightAr) ? s.SiteNameHighlightEn : s.SiteNameHighlightAr)
                : s.SiteNameHighlightEn;
            return $"{a}{b}".Trim();
        }
    }
}
