using Microsoft.AspNetCore.Identity;

namespace UserAccountAPI.Models
{
    public class ApplicationRole : IdentityRole<int>
    {
        public string? Description { get; set; }
    }
}