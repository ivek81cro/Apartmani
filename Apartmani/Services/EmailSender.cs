using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Apartmani.Services
{
    public class EmailSender : IEmailSender, IContactEmailSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly bool _enableSsl;
        private readonly string _userName;
        private readonly string _password;

        public EmailSender(string host, int port, bool enableSSL, string userName, string password)
        {
            _host = host;
            _port = port;
            _enableSsl = enableSSL;
            _userName = userName;
            _password = password;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var client = CreateClient();
            using var mail = new MailMessage(_userName, email, subject, htmlMessage) { IsBodyHtml = true };
            await client.SendMailAsync(mail);
        }

        public async Task SendContactAsync(string recipient, string name, string email, string message)
        {
            using var client = CreateClient();
            using var mail = new MailMessage(_userName, recipient)
            {
                Subject = "Upit sa stranice Apartmani Tokić",
                Body = $"<p>Ime: {WebUtility.HtmlEncode(name)}</p><p>E-mail: {WebUtility.HtmlEncode(email)}</p><p>Poruka: {WebUtility.HtmlEncode(message)}</p>",
                IsBodyHtml = true
            };
            mail.ReplyToList.Add(new MailAddress(email));
            await client.SendMailAsync(mail);
        }

        private SmtpClient CreateClient() => new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_userName, _password),
            EnableSsl = _enableSsl
        };
    }
}
