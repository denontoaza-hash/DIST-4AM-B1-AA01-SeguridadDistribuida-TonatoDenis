using System.ComponentModel.DataAnnotations;

namespace Auth.Api.DTOs
{
    public class RegistroRequestDto
    {
        [Required]
        [StringLength(120)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "La confirmación de contraseña no coincide.")]
        public string ConfirmarPassword { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }
    }

    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequestDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    public class RecuperarPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;
    }

    public class RestablecerPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NuevaPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NuevaPassword), ErrorMessage = "La confirmación de contraseña no coincide.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }

    public class CambiarPasswordRequestDto
    {
        [Required]
        public string PasswordActual { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NuevaPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NuevaPassword), ErrorMessage = "La confirmación de contraseña no coincide.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }

    public class CambiarRolRequestDto
    {
        [Required]
        public string Rol { get; set; } = string.Empty;
    }

    public class CambiarEstadoCuentaRequestDto
    {
        [Required]
        public bool EstaActivo { get; set; }
    }
}