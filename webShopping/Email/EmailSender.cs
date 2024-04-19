using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

using System.ComponentModel.DataAnnotations;

namespace webShopping.Email
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        { 
            throw new NotImplementedException();
        }
        //    var client = new SendGridClient(Options.SendGridKey);
        //    var message = new SendGridMessage()
        //    {
        //        From = new EmailAddress("mly751059@gmail.com","sager"),
        //        Subject = subject,
        //        PlainTextContent = htmlMessage,
        //        HtmlContent = htmlMessage
        //    };
        //    message.AddTo(new EmailAddress(email));
        //    try
        //    {
        //        return client.SendEmailAsync(message);
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //    return null;
        //}
        //public EmailOptions Options { get; set; }   
        //public EmailSender(IOptions<EmailOptions> options)
        //{
        //    Options=options.Value;
        //}
    }
}
