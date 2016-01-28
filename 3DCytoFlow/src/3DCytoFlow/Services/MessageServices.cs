using System;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.AspNet.Identity;
using System.Net;

namespace _3DCytoFlow.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link http://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        public async Task SendEmailAsync(string email, string subject, string message, string username, string psw)
        {
            await SendMail(new Message { Body = message, Subject = subject }, username, psw, email);
        }

        public async Task SendMail(Message message, string username, string psw, string to = null)
        {
            var text = $"{message.Subject}: {message.Body}";
            var html = message.Body;

            var mail = new MailAddress(username);
            var msg = new MailMessage { From = mail };
            msg.To.Add(to == null ? mail : new MailAddress(to));

            msg.Subject = message.Subject;
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

            var smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
            var credentials = new NetworkCredential(mail.Address, psw);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = credentials;
            smtpClient.EnableSsl = true;
            await smtpClient.SendMailAsync(msg);
        }

        public class Message
        {
            public string Body { get; set; }
            public string Subject { get; set; }
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
