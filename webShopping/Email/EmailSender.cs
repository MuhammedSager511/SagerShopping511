using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace webShopping.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtp = _configuration.GetSection("Smtp");
            var host = smtp["Host"];

            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogInformation(
                    "[Email — dev mode, configure Smtp:Host to send]\nTo: {Email}\nSubject: {Subject}\n{Body}",
                    email, subject, htmlMessage);
                return;
            }

            var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
            var from = smtp["From"] ?? smtp["User"] ?? "noreply@sagershop.com";
            var enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;

            using var message = new MailMessage
            {
                From = new MailAddress(from, smtp["FromName"] ?? "SagerShop"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrEmpty(smtp["User"])
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(smtp["User"], smtp["Password"])
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}: {Subject}", email, subject);
        }
    }
}
