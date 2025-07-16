using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.API.Models
{
    public class ApplicationUser : IdentityUser
    {
       
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();


    }
}
