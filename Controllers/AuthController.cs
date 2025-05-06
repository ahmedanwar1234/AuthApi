using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Services.Interfaces;
using UserAccountAPI.Data;
using UserAccountAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace UserAccountAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AuthController(IAuthService authService, IEmailService emailService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _authService = authService;
            _emailService = emailService;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (result, user) = await _authService.RegisterUserAsync(model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Create corresponding Patient record
            var patient = new Patient
            {
                FullName = $"{model.FirstName} {model.LastName}",
                DateOfBirth = model.DateOfBirth ?? DateTime.UtcNow,
                Gender = model.Gender,
                PhoneNumber = model.PhoneNumber,
                AdmissionDate = DateTime.UtcNow,
                HasAppointments = false,
                UserId = user.Id
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Generate and send 6-digit confirmation code
            await _authService.GenerateAndSendEmailConfirmationCodeAsync(user);

            // Generate tokens immediately after registration
            var authResponse = await _authService.GenerateTokenAsync(user);

            // Return the user object including Patient data
            return Ok(authResponse.User);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (result, user, roles) = await _authService.LoginUserAsync(model);

            if (result.IsLockedOut)
            {
                return BadRequest(new { message = "Your account is locked. Please try again later or contact support." });
            }

            if (!result.Succeeded)
            {
                if (user != null && !await _authService.IsEmailConfirmedAsync(user.Id))
                {
                    return BadRequest(new { message = "Please confirm your email injustice logging in." });
                }
                return BadRequest(new { message = "Invalid login attempt." });
            }

            // Generate auth response with tokens
            var authResponse = await _authService.GenerateTokenAsync(user);

            // Return the user object with all token information included
            return Ok(authResponse.User);
        }

        // Other endpoints (logout, forgot-password, etc.) remain unchanged
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDTO model)
        {
            if (string.IsNullOrEmpty(model.AccessToken))
            {
                return BadRequest(new { message = "Access token is required." });
            }

            await _authService.LogoutAsync(model.AccessToken);

            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Ok(new { message = "If your email is registered, you will receive a password reset." });
            }

            var newPassword = GenerateRandomPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            await _emailService.SendEmailAsync(
                user.Email,
                "New Password",
                $"Hello {user.UserName},\n\nYour new password is: {newPassword}\n\nPlease change it after logging in."
            );

            return Ok(new { message = "A new password has been sent to your email address." });
        }

        private string GenerateRandomPassword()
        {
            const int length = 10;
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@$?_-";

            var random = new Random();
            var chars = new char[length];

            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(chars);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid or missing user ID." });
            }

            var result = await _authService.ChangePasswordAsync(userId, model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmationDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ConfirmEmailAsync(model);

            if (!result)
            {
                return BadRequest(new { message = "Invalid or expired confirmation code." });
            }

            return Ok(new { message = "Email confirmed successfully. You can now log in." });
        }
        // Controllers/AuthController.cs
        [HttpPost("resend-confirmation-code")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmationCode([FromBody] ResendConfirmationCodeDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || user.EmailConfirmed)
            {
                return Ok(new { message = "If your email is registered and not yet confirmed, a new code has been sent." });
            }

            await _authService.GenerateAndSendEmailConfirmationCodeAsync(user);
            return Ok(new { message = "A new confirmation code has been sent to your email." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authResponse = await _authService.RefreshTokenAsync(model);

            if (authResponse == null)
            {
                return BadRequest(new { message = "Invalid token." });
            }

            return Ok(authResponse.User);
        }

        [HttpGet("user-info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid." });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var authResponse = await _authService.GenerateTokenAsync(user);
            return Ok(authResponse.User);
        }

        [HttpGet("check-role")]
        [Authorize]
        public async Task<IActionResult> CheckUserRole(string role)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid." });
            }

            var roles = await _authService.GetUserRolesAsync(userId);
            var hasRole = roles.Contains(role);

            return Ok(new { hasRole });
        }
    }
}