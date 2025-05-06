using System.Threading.Tasks;

namespace UserAccountAPI.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendPasswordResetEmailAsync(string email, string callbackUrl, int userId, string rawToken);
        Task SendEmailConfirmationAsync(string email, string code);
    }
}
