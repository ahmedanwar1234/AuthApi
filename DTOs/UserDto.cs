using System;

namespace UserAccountAPI.DTOs
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string NationalId { get; set; }
        public string DateOfBirth { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public PatientDTO Patient { get; set; }
    }

    public class UpdateUserDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string NationalId { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}