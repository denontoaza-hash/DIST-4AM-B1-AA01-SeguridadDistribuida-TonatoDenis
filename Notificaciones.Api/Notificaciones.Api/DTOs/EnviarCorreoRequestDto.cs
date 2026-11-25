using System.ComponentModel.DataAnnotations;

namespace Notificaciones.Api.DTOs
{
    public class EnviarCorreoRequestDto
    {
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
        public string CuerpoTexto { get; set; } = string.Empty;
    }
}