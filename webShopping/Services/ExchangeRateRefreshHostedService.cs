namespace webShopping.Services
{
    public class ExchangeRateRefreshHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExchangeRateRefreshHostedService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6);

        public ExchangeRateRefreshHostedService(
            IServiceProvider serviceProvider,
            ILogger<ExchangeRateRefreshHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RefreshAsync(stoppingToken);

            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAsync(stoppingToken);
            }
        }

        private async Task RefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var exchangeRates = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
                await exchangeRates.RefreshAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to refresh exchange rates.");
            }
        }
    }
}
