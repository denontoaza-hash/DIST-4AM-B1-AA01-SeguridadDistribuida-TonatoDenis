using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notificaciones.Api.Data;
using Notificaciones.Api.DTOs;
using Notificaciones.Api.Services;

namespace Notificaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly ICorreoService _correoService;
        private readonly NotificacionesDbContext _context;

        public NotificacionesController(ICorreoService correoService, NotificacionesDbContext context)
        {
            _correoService = correoService;
            _context = context;
        }

        // POST: api/Notificaciones/enviar
        [HttpPost("enviar")]
        public async Task<IActionResult> Enviar([FromBody] EnviarCorreoRequestDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var esServicioAutorizado = User.HasClaim("scope", "notificaciones.internas") ||
                                     User.IsInRole("Administrador");

            if (!esServicioAutorizado)
                return Forbid();

            var (enviado, errorSeguro) = await _correoService.EnviarAsync(dto, ct);

            if (!enviado)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    mensaje = errorSeguro ?? "No se pudo enviar la notificación."
                });
            }

            return Ok(new { mensaje = "Notificación enviada y registrada." });
        }

        // GET: api/Notificaciones/historial
        [HttpGet("historial")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ObtenerHistorial(CancellationToken ct)
        {
            var historial = await _context.EnviosCorreo
                .AsNoTracking()
                .OrderByDescending(e => e.FechaEnvio)
                .Select(e => new
                {
                    e.IdEnvio,
                    e.Destinatario,
                    e.Asunto,
                    e.TipoNotificacion,
                    e.Estado,
                    e.FechaEnvio
                })
                .ToListAsync(ct);

            return Ok(historial);
        }
    }
}