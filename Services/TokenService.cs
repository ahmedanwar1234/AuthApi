using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UserAccountAPI.Data;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;
using UserAccountAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace UserAccountAPI.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TokenService(
            IConfiguration configuration,
            ApplicationDbContext context,
            IMapper mapper)
        {
            _configuration = configuration;
            _context = context;
            _mapper = mapper;
        }

        public async Task<AuthResponseDTO> GenerateTokensAsync(ApplicationUser user, IList<string> roles = null)
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("firstName", user.FirstName ?? string.Empty),
        new Claim("lastName", user.LastName ?? string.Empty)
    };

            // Add roles to claims
            if (roles != null && roles.Any())
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var expiryInMinutesString = jwtSettings["AccessTokenExpiryMinutes"];
            var tokenExpiryMinutes = int.TryParse(expiryInMinutesString, out var minutes) ? minutes : 15;
            var tokenExpiry = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = tokenExpiry,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            var refreshToken = GenerateRefreshToken();

            // Store refresh token in database
            var storedRefreshToken = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(storedRefreshToken);
            await _context.SaveChangesAsync();

            // Create UserDTO with all the needed properties
            var userDto = _mapper.Map<UserDTO>(user);
            userDto.Role = roles?.FirstOrDefault() ?? string.Empty;
            userDto.AccessToken = accessToken;
            userDto.RefreshToken = refreshToken;
            userDto.ExpiresAt = tokenExpiry;
            var patient = await _context.Patients
       .Where(p => p.UserId == user.Id)
       .FirstOrDefaultAsync();

            PatientDTO patientDto = null;
            if (patient != null)
            {
                patientDto = new PatientDTO
                {
                    Id = patient.Id,
                    FullName = patient.FullName,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    PhoneNumber = patient.PhoneNumber,
                    AdmissionDate = patient.AdmissionDate,
                    HasAppointments = patient.HasAppointments,
                    UserId = patient.UserId
                };
            }

            // تحويل بيانات المستخدم إلى UserDTO
                var userDTO = new UserDTO
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                NationalId = user.NationalId,
                DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
                Role = roles?.FirstOrDefault() ?? string.Empty,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = tokenExpiry,
                Patient = patientDto // ✅ هنا بقى نربط بيانات المريض
            };

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = tokenExpiry,
                User = userDto
            };
        }
        public async Task<AuthResponseDTO> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                return null;
            }

            var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return null; // أو رسالة خطأ مناسبة حسب الحاجة
            }
            var storedRefreshToken = _context.RefreshTokens
                .FirstOrDefault(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (storedRefreshToken == null ||
                storedRefreshToken.IsUsed ||
                storedRefreshToken.IsRevoked ||
                storedRefreshToken.ExpiryDate < DateTime.UtcNow)
            {
                return null;
            }

            storedRefreshToken.IsUsed = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return null;
            }

            // Get user roles from claims
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return await GenerateTokensAsync(user, roles);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false // Allow expired tokens
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }
    }
}