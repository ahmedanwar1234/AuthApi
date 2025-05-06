using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;

namespace UserAccountAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(IdentityResult Result, ApplicationUser User)> RegisterUserAsync(RegisterDTO model);
        Task<(SignInResult Result, ApplicationUser User, IList<string> Roles)> LoginUserAsync(LoginDTO model);
        Task LogoutAsync(string accessToken);
        Task<bool> ForgotPasswordAsync(string email);
        Task<IdentityResult> ChangePasswordAsync(int userId, ChangePasswordDTO model);
        Task<bool> ConfirmEmailAsync(EmailConfirmationDTO model);
        Task<AuthResponseDTO> GenerateTokenAsync(ApplicationUser user);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO model);
        Task<bool> IsEmailConfirmedAsync(int userId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> GetUserByIdAsync(int  id);
        Task<IList<string>> GetUserRolesAsync(int userId);
        Task<bool> AddUserToRoleAsync(int userId, string role);
        Task<bool> RemoveUserFromRoleAsync(int userId, string role);
        Task GenerateAndSendEmailConfirmationCodeAsync(ApplicationUser user);
    }
    public class AuthResponse
    {
        public object User { get; set; }
    }
}