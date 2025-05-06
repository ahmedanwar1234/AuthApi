// Services/EmailService.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Threading.Tasks;
using UserAccountAPI.Services.Interfaces;

namespace UserAccountAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress(_configuration["EmailSettings:SenderName"],
                                                   _configuration["EmailSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;

            emailMessage.Body = new TextPart("html")
            {
                Text = message
            };

            using var client = new SmtpClient();

            await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
                              int.Parse(_configuration["EmailSettings:Port"]),
                              SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(_configuration["EmailSettings:Username"],
                                  _configuration["EmailSettings:Password"]);

            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }

        public async Task SendPasswordResetEmailAsync(string email, string callbackUrl, int userId, string rawToken)
        {
            var subject = "Reset Your Password";
            var message = $@"
                <h1>Reset Your Password</h1>
                <p>Please reset your password by clicking the link below:</p>
                <p><a href='{callbackUrl}'>Reset Password</a></p>
                <p>If you're testing locally, use these values:</p>
                <p>UserID: {userId}</p>
                <p>Raw Token: {rawToken}</p>
                <p>If you did not request a password reset, please ignore this email.</p>
                <p>This link will expire in 24 hours.</p>";
            await SendEmailAsync(email, subject, message);
        }

        public async Task SendEmailConfirmationAsync(string email, string code)
        {
            var subject = "Confirm Your Email";
            var message = $@"
                <h1>Thanks for signing up!</h1>
                <p>Please confirm your email address by entering the following 6-digit code in the confirmation page:</p>
                <p><strong>{code}</strong></p>
                <p>This code will expire in 30 minutes.</p>
                <p>If you did not create an account, please ignore this email.</p>";
            await SendEmailAsync(email, subject, message);
        }
    }
}