using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Rev1.API.Security.Business.Contract;
using Rev1.API.Security.Utils.Configuration;

namespace Rev1.API.Security.Business
{
    public class EmailService : IEmailService
    {
        private readonly AppSettings _appSettings;

        public EmailService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public void Send(string to, string subject, string html, string from = null)
        {
            // create message
            var email = new MimeMessage();
            //email.From.Add(MailboxAddress.Parse(from ?? _appSettings.EmailFrom));
            email.From.Add(MailboxAddress.Parse(from ?? "info@aspnet-core-signup-verification-api.com"));
            email.To.Add(MailboxAddress.Parse(to ?? "victoruzo2408@gmail.com"));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = html };



            // send email
            //using var smtp = new SmtpClient();
            //smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, SecureSocketOptions.StartTls);
            //smtp.Authenticate(_appSettings.SmtpUser, _appSettings.SmtpPass);

            //smtp.Connect("smtp.ethereal.email", 587, SecureSocketOptions.StartTls);
            //smtp.Authenticate("pinkie.pfannerstill48@ethereal.email", "cteNFqE5n2YDgt5qvt");

            //smtp.Send(email);
            //smtp.Disconnect(true);

            //using (var smtp = new SmtpClient())
            //{
            //    smtp.Connect("smtp.ethereal.email", 587, SecureSocketOptions.StartTls);
            //    smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            //    smtp.Authenticate("dennis.johnston53@ethereal.email", "5PW3mtZMa6YVgyHpsW");                
            //    smtp.Send(email);
            //    smtp.Disconnect(true);
            //}
        }
    }
}
