namespace webShopping.Services
{
    public interface ICurrencyService
    {
        string CurrentCurrency { get; }
        string CurrentSymbol { get; }
        double GetRate(string currencyCode);
        double ConvertFromUsd(double amountUsd, string? currencyCode = null);
        string Format(double amountUsd);
        string FormatInCurrency(double amountUsd, string currencyCode);
        void SetCurrency(string code);
        IReadOnlyList<(string Code, string Symbol, string Name)> GetSupportedCurrencies();
        DateTime? RatesLastUpdated { get; }
        string? RatesSource { get; }
    }
}
