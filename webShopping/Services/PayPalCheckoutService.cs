using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using webShopping.Models;

namespace webShopping.Services
{
    public interface IPayPalCheckoutService
    {
        bool IsConfigured { get; }
        Task<PayPalCreateResult> CreateOrderAsync(OrderHeader order, string returnUrl, string cancelUrl);
        Task<PayPalCaptureResult> CaptureOrderAsync(string paypalOrderId);
    }

    public record PayPalCreateResult(bool Success, string? ApprovalUrl, string? OrderId, string? ErrorMessage);
    public record PayPalCaptureResult(bool Success, string? CaptureId, string? ErrorMessage);

    public class PayPalCheckoutService : IPayPalCheckoutService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PayPalCheckoutService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_configuration["PayPal:ClientId"]) &&
            !string.IsNullOrWhiteSpace(_configuration["PayPal:Secret"]);

        public async Task<PayPalCreateResult> CreateOrderAsync(OrderHeader order, string returnUrl, string cancelUrl)
        {
            if (!IsConfigured)
                return new PayPalCreateResult(false, null, null, "PayPal is not configured.");

            try
            {
                var token = await GetAccessTokenAsync();
                if (token == null)
                    return new PayPalCreateResult(false, null, null, "PayPal authentication failed.");

                var amountUsd = order.orderTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                var payload = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            reference_id = order.Id.ToString(),
                            description = $"SagerShop Order #{order.Id}",
                            amount = new { currency_code = "USD", value = amountUsd }
                        }
                    },
                    application_context = new
                    {
                        return_url = returnUrl,
                        cancel_url = cancelUrl,
                        brand_name = "SagerShop",
                        user_action = "PAY_NOW"
                    }
                };

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var json = JsonSerializer.Serialize(payload);
                var response = await client.PostAsync(
                    $"{GetBaseUrl()}/v2/checkout/orders",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return new PayPalCreateResult(false, null, null, body);

                using var doc = JsonDocument.Parse(body);
                var orderId = doc.RootElement.GetProperty("id").GetString();
                string? approveUrl = null;
                foreach (var link in doc.RootElement.GetProperty("links").EnumerateArray())
                {
                    if (link.GetProperty("rel").GetString() == "approve")
                    {
                        approveUrl = link.GetProperty("href").GetString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(approveUrl))
                    return new PayPalCreateResult(false, null, orderId, "PayPal approval URL not found.");

                return new PayPalCreateResult(true, approveUrl, orderId, null);
            }
            catch (Exception ex)
            {
                return new PayPalCreateResult(false, null, null, ex.Message);
            }
        }

        public async Task<PayPalCaptureResult> CaptureOrderAsync(string paypalOrderId)
        {
            if (!IsConfigured)
                return new PayPalCaptureResult(false, null, "PayPal is not configured.");

            try
            {
                var token = await GetAccessTokenAsync();
                if (token == null)
                    return new PayPalCaptureResult(false, null, "PayPal authentication failed.");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.PostAsync(
                    $"{GetBaseUrl()}/v2/checkout/orders/{paypalOrderId}/capture",
                    new StringContent("{}", Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return new PayPalCaptureResult(false, null, body);

                using var doc = JsonDocument.Parse(body);
                var status = doc.RootElement.GetProperty("status").GetString();
                if (status != "COMPLETED")
                    return new PayPalCaptureResult(false, null, $"PayPal status: {status}");

                var captureId = doc.RootElement
                    .GetProperty("purchase_units")[0]
                    .GetProperty("payments")
                    .GetProperty("captures")[0]
                    .GetProperty("id").GetString();

                return new PayPalCaptureResult(true, captureId, null);
            }
            catch (Exception ex)
            {
                return new PayPalCaptureResult(false, null, ex.Message);
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            var clientId = _configuration["PayPal:ClientId"]!;
            var secret = _configuration["PayPal:Secret"]!;
            var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await client.PostAsync(
                $"{GetBaseUrl()}/v1/oauth2/token",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" }));

            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        private string GetBaseUrl()
        {
            var mode = _configuration["PayPal:Mode"] ?? "sandbox";
            return mode.Equals("live", StringComparison.OrdinalIgnoreCase)
                ? "https://api-m.paypal.com"
                : "https://api-m.sandbox.paypal.com";
        }
    }
}
