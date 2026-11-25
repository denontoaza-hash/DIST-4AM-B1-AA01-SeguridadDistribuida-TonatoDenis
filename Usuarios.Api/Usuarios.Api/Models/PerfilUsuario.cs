using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Usuarios.Api.Models
{
    [Table("Perfiles")]
    public class PerfilUsuario
    {
        [Key]
        public int IdPerfil { get; set; }

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

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }
    }
}