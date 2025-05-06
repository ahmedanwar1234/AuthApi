using System;
using System.ComponentModel.DataAnnotations;

namespace UserAccountAPI.DTOs
{
    public class RegisterDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        [Required]
        public string NationalId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [RegularExpression("^(Male|Female)$", ErrorMessage = "Gender must be either 'Male' or 'Female'.")]
        public string Gender { get; set; }
    }

    public class LoginDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
    public class LogoutDTO
    {
        public string AccessToken { get; set; }
    }
    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ChangePasswordDTO
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string NewPassword { get; set; }

    }

    public class EmailConfirmationDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The code must be a 6-digit number.")]
        public string Code { get; set; }
    }
    public class ResendConfirmationCodeDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class RefreshTokenDTO
    {
        [Required]
        public string AccessToken { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }

    public class AuthResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserDTO User { get; set; }
    }

}