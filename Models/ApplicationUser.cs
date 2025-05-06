// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System;
using UserAccountAPI.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public string NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; }
    public Patient Patient { get; set; }

    public string? EmailConfirmationCode { get; set; }
    public DateTime? EmailConfirmationCodeExpiration { get; set; }
}