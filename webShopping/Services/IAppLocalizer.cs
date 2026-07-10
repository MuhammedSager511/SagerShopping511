namespace webShopping.Services
{
    public interface IAppLocalizer
    {
        string this[string key] { get; }
        string Get(string key);
        string Get(string key, string culture);
        bool IsArabic { get; }
        string CurrentLanguage { get; }
    }
}
