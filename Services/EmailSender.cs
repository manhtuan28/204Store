using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace _204Store.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mailSettings = _configuration.GetSection("MailSettings");

            string mail = mailSettings["Mail"];
            string password = mailSettings["Password"];
            string host = mailSettings["Host"];
            int port = int.Parse(mailSettings["Port"]);

            var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(mail, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(from: mail, to: email, subject, htmlMessage);
            mailMessage.IsBodyHtml = true;

            return client.SendMailAsync(mailMessage);
        }
    }
}