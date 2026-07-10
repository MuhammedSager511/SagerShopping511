namespace webShopping.Services
{
    public interface IExchangeRateService
    {
        double GetRate(string currencyCode);
        DateTime? LastUpdated { get; }
        string? Source { get; }
        Task RefreshAsync(CancellationToken cancellationToken = default);
    }
}
