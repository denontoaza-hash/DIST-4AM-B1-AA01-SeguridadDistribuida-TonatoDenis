using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notificaciones.Api.Models
{
    [Table("EnviosCorreo")]
    public class EnvioCorreo
    {
        [Key]
        public int IdEnvio { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Destinatario { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Asunto { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TipoNotificacion { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = string.Empty;

        [StringLength(500)]
        public string? DetalleError { get; set; }

        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
    }
}