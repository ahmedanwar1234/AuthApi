using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;
using UserAccountAPI.Services.Interfaces;
using UserAccountAPI.Common;
using UserAccountAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace UserAccountAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration configuration,
            IMapper mapper,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;
            _mapper = mapper;
            _context = context;
        }

        public async Task<(IdentityResult Result, ApplicationUser User)> RegisterUserAsync(RegisterDTO model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                NationalId = model.NationalId,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Check if role exists first, create it if it doesn't
                if (!await _roleManager.RoleExistsAsync(UserRoles.Patient))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = UserRoles.Patient });
                }

                // Assign the Patient role
                await _userManager.AddToRoleAsync(user, UserRoles.Patient);

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

                // Send email confirmation with 6-digit code
                await GenerateAndSendEmailConfirmationCodeAsync(user);
            }

            return (result, user);
        }

        public async Task<(SignInResult Result, ApplicationUser User, IList<string> Roles)> LoginUserAsync(LoginDTO model)
        {
            var user = await _context.Users
                .Include(u => u.Patient) // Include Patient data
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return (SignInResult.Failed, null, null);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user) &&
                _configuration.GetValue<bool>("RequireConfirmedEmail"))
            {
                return (SignInResult.Failed, user, null);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                return (result, user, roles);
            }

            return (result, user, null);
        }

        public async Task<AuthResponseDTO> GenerateTokenAsync(ApplicationUser user)
        {
            // Fetch user with Patient data
            user = await _context.Users
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            var tokenResponse = await _tokenService.GenerateTokensAsync(user, roles);

            // Map user to DTO and include Patient data
            var userDto = _mapper.Map<UserDTO>(user);
            userDto.Role = roles.FirstOrDefault();
            userDto.AccessToken = tokenResponse.AccessToken;
            userDto.RefreshToken = tokenResponse.RefreshToken;
            userDto.ExpiresAt = tokenResponse.ExpiresAt;

            return new AuthResponseDTO { User = userDto };
        }

        public async Task<ApplicationUser> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Patient) // Include Patient data
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task LogoutAsync(string accessToken)
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return true;
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var clientUrl = _configuration["ClientUrl"];
            var resetLink = $"{clientUrl}/reset-password?email={HttpUtility.UrlEncode(email)}&token={encodedToken}";

            await _emailService.SendPasswordResetEmailAsync(email, resetLink, user.Id, token);

            return true;
        }

        public async Task<IdentityResult> ChangePasswordAsync(int userId, ChangePasswordDTO model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            return await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        }

        public async Task GenerateAndSendEmailConfirmationCodeAsync(ApplicationUser user)
        {
            // Generate 6-digit code
            var code = new Random().Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(30); // Code expires in 30 minutes

            // Store code and expiration
            user.EmailConfirmationCode = code;
            user.EmailConfirmationCodeExpiration = expiration;
            await _userManager.UpdateAsync(user);

            // Send email with code
            await _emailService.SendEmailConfirmationAsync(user.Email, code);
        }

        public async Task<bool> ConfirmEmailAsync(EmailConfirmationDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return false;

            if (user.EmailConfirmationCode != model.Code || user.EmailConfirmationCodeExpiration < DateTime.UtcNow)
                return false;

            // Mark email as confirmed
            user.EmailConfirmed = true;
            user.EmailConfirmationCode = null; // Clear the code
            user.EmailConfirmationCodeExpiration = null;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO model)
        {
            return await _tokenService.RefreshTokenAsync(model.AccessToken, model.RefreshToken);
        }

        public async Task<bool> IsEmailConfirmedAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

             return await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IList<string>> GetUserRolesAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new List<string>();
            }

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> AddUserToRoleAsync(int userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = role });
            }

            var result = await _userManager.AddToRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task<bool> RemoveUserFromRoleAsync(int userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            return result.Succeeded;
        }
    }
}