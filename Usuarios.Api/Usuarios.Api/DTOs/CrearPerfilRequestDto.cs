using System.ComponentModel.DataAnnotations;

namespace Usuarios.Api.DTOs
{
    public class CrearPerfilRequestDto
    {
        [Required]
        [StringLength(100)]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Correo { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }
    }
}