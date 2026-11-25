using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombres { get; set; } = string.Empty;
        public bool EstaActivo { get; set; } = true;
        public string? GoogleSubjectId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}