using System.ComponentModel.DataAnnotations;

namespace Usuarios.Api.DTOs
{
    public class ActualizarPerfilRequestDto
    {
        [Required]
        [StringLength(120)]
        public string Nombres { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }
    }
}