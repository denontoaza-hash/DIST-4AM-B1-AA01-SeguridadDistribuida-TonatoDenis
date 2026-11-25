using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Usuarios.Api.Data;
using Usuarios.Api.DTOs;
using Usuarios.Api.Models;

namespace Usuarios.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PerfilesController : ControllerBase
    {
        private readonly UsuariosDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PerfilesController(
            UsuariosDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private string GenerarTokenServicioNotificaciones()
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
            var issuer = jwtSection["Issuer"] ?? "Auth.Api";
            var audience = jwtSection["Audience"] ?? "SistemaComercial.Api";

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "usuarios-api-servicio"),
                new(ClaimTypes.NameIdentifier, "usuarios-api-servicio"),
                new("scope", "notificaciones.internas"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string? ObtenerUsuarioIdToken()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");
        }

        private bool EsAdministrador()
        {
            return User.IsInRole("Administrador");
        }

        private bool EsServicioAutorizado()
        {
            return User.HasClaim("scope", "usuarios.internos") || EsAdministrador();
        }

        // POST: api/Perfiles
        // Crea el perfil vinculado por UsuarioId (llamado por Auth.Api durante el registro)
        [HttpPost]
        public async Task<IActionResult> CrearPerfil([FromBody] CrearPerfilRequestDto dto, CancellationToken ct)
        {
            if (!EsServicioAutorizado())
                return Forbid();

            // Simulación de latencia para prueba de timeout distribuido (Caso 14)
            if (Request.Headers.TryGetValue("X-Simulate-Delay", out var delayHeader) &&
                int.TryParse(delayHeader, out var delayMs) && delayMs > 0)
            {
                await Task.Delay(delayMs, ct);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioIdNormalizado = dto.UsuarioId.Trim();
            var correoNormalizado = dto.Correo.Trim().ToLowerInvariant();

            var yaExiste = await _context.Perfiles
                .AnyAsync(p => p.UsuarioId == usuarioIdNormalizado, ct);

            if (yaExiste)
                return Conflict(new { mensaje = "Ya existe un perfil registrado para este UsuarioId." });

            var perfil = new PerfilUsuario
            {
                UsuarioId = usuarioIdNormalizado,
                Nombres = dto.Nombres.Trim(),
                Correo = correoNormalizado,
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                Ciudad = string.IsNullOrWhiteSpace(dto.Ciudad) ? null : dto.Ciudad.Trim(),
                FechaCreacion = DateTime.UtcNow
            };

            _context.Perfiles.Add(perfil);
            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(ObtenerPerfilPorUsuarioId), new { usuarioId = perfil.UsuarioId }, perfil);
        }

        // GET: api/Perfiles/mi-perfil
        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil(CancellationToken ct)
        {
            var usuarioId = ObtenerUsuarioIdToken();
            if (string.IsNullOrWhiteSpace(usuarioId))
                return Unauthorized(new { mensaje = "Token sin identificador de usuario." });

            var perfil = await _context.Perfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

            if (perfil == null)
                return NotFound(new { mensaje = "Perfil no encontrado." });

            return Ok(perfil);
        }

        // GET: api/Perfiles/{usuarioId}
        [HttpGet("{usuarioId}")]
        public async Task<IActionResult> ObtenerPerfilPorUsuarioId(string usuarioId, CancellationToken ct)
        {
            var usuarioIdToken = ObtenerUsuarioIdToken();
            if (string.IsNullOrWhiteSpace(usuarioIdToken))
                return Unauthorized(new { mensaje = "Token sin identificador de usuario." });

            if (!EsAdministrador() && usuarioIdToken != usuarioId)
                return Forbid();

            var perfil = await _context.Perfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

            if (perfil == null)
                return NotFound(new { mensaje = "Perfil no encontrado." });

            return Ok(perfil);
        }

        // GET: api/Perfiles
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ListarPerfiles(CancellationToken ct)
        {
            var perfiles = await _context.Perfiles
                .AsNoTracking()
                .OrderBy(p => p.IdPerfil)
                .ToListAsync(ct);

            return Ok(perfiles);
        }

        // PUT: api/Perfiles/{usuarioId}
        // Solo permite modificar datos personales permitidos del propio perfil (o Administrador)
        [HttpPut("{usuarioId}")]
        public async Task<IActionResult> ActualizarPerfil(
            string usuarioId,
            [FromBody] ActualizarPerfilRequestDto dto,
            CancellationToken ct)
        {
            var usuarioIdToken = ObtenerUsuarioIdToken();
            if (string.IsNullOrWhiteSpace(usuarioIdToken))
                return Unauthorized(new { mensaje = "Token sin identificador de usuario." });

            if (!EsAdministrador() && usuarioIdToken != usuarioId)
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var perfil = await _context.Perfiles
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

            if (perfil == null)
                return NotFound(new { mensaje = "Perfil no encontrado." });

            perfil.Nombres = dto.Nombres.Trim();
            perfil.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            perfil.Ciudad = string.IsNullOrWhiteSpace(dto.Ciudad) ? null : dto.Ciudad.Trim();
            perfil.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            // Opcionalmente notificar cambio de datos vía Notificaciones.Api
            try
            {
                var notifClient = _httpClientFactory.CreateClient("NotificacionesApiClient");
                var token = GenerarTokenServicioNotificaciones();
                notifClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    Destinatario = perfil.Correo,
                    Asunto = "Actualización de datos de perfil",
                    TipoNotificacion = "ActualizacionPerfil",
                    CuerpoTexto = $"Hola {perfil.Nombres}, te informamos que los datos de tu perfil en el sistema comercial han sido actualizados satisfactoriamente."
                };

                await notifClient.PostAsJsonAsync("api/Notificaciones/enviar", payload, ct);
            }
            catch
            {
                // La actualización ya se persistió; no se interrumpe la respuesta
            }

            return Ok(perfil);
        }
    }
}