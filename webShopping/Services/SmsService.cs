namespace webShopping.Services
{
    public interface ISmsService
    {
        Task SendAsync(string phoneNumber, string message);
    }

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return;

            var sms = _configuration.GetSection("Sms");
            var provider = sms["Provider"] ?? "Log";

            if (!sms.GetValue("Enabled", false) || provider.Equals("Log", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[SMS — dev mode] To: {Phone} | {Message}", phoneNumber, message);
                return;
            }

            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                await SendViaTwilioAsync(sms, phoneNumber, message);
                return;
            }

            _logger.LogWarning("Unknown SMS provider '{Provider}'. Message not sent.", provider);
        }

        private async Task SendViaTwilioAsync(IConfigurationSection sms, string phoneNumber, string message)
        {
            var accountSid = sms["TwilioAccountSid"];
            var authToken = sms["TwilioAuthToken"];
            var from = sms["TwilioFromNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(from))
            {
                _logger.LogWarning("Twilio SMS not configured. Message to {Phone} logged only.", phoneNumber);
                _logger.LogInformation("[SMS] {Message}", message);
                return;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = phoneNumber,
                ["From"] = from,
                ["Body"] = message
            });

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                _logger.LogError("Twilio SMS failed: {Status} {Body}", response.StatusCode, await response.Content.ReadAsStringAsync());
            else
                _logger.LogInformation("SMS sent to {Phone}", phoneNumber);
        }
    }
}
