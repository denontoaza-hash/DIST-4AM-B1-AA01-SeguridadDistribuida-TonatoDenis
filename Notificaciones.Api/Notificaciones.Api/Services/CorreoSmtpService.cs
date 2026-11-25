using System.Net;
using System.Net.Mail;
using Notificaciones.Api.Data;
using Notificaciones.Api.DTOs;
using Notificaciones.Api.Models;

namespace Notificaciones.Api.Services
{
    public interface ICorreoService
    {
        Task<(bool Enviado, string? ErrorSeguro)> EnviarAsync(EnviarCorreoRequestDto dto, CancellationToken ct = default);
    }

    public class CorreoSmtpService : ICorreoService
    {
        private readonly NotificacionesDbContext _context;
        private readonly IConfiguration _configuration;

        public CorreoSmtpService(NotificacionesDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool Enviado, string? ErrorSeguro)> EnviarAsync(
            EnviarCorreoRequestDto dto,
            CancellationToken ct = default)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"];
            var portStr = smtpSection["Port"];
            var enableSslStr = smtpSection["EnableSsl"];
            var username = smtpSection["User"];
            var password = smtpSection["Password"];
            var from = smtpSection["FromEmail"] ?? username;
            var fromName = smtpSection["FromName"];

            var registro = new EnvioCorreo
            {
                Destinatario = dto.Destinatario.Trim(),
                Asunto = dto.Asunto.Trim(),
                TipoNotificacion = dto.TipoNotificacion.Trim(),
                FechaEnvio = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(host) ||
                !int.TryParse(portStr, out var port) ||
                string.IsNullOrWhiteSpace(from))
            {
                registro.Estado = "Fallido";
                registro.DetalleError = "Configuración SMTP incompleta en el servidor.";
                _context.EnviosCorreo.Add(registro);
                await _context.SaveChangesAsync(ct);
                return (false, "No se pudo completar el envío de correo en este momento.");
            }

            bool enableSsl = !bool.TryParse(enableSslStr, out var sslParsed) || sslParsed;

            try
            {
                using var mensaje = new MailMessage();
                mensaje.From = !string.IsNullOrWhiteSpace(fromName)
                    ? new MailAddress(from, fromName)
                    : new MailAddress(from);
                mensaje.To.Add(registro.Destinatario);
                mensaje.Subject = registro.Asunto;
                mensaje.Body = dto.CuerpoTexto;
                mensaje.IsBodyHtml = false;

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Timeout = 10000
                };

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }

                ct.ThrowIfCancellationRequested();
                await client.SendMailAsync(mensaje, ct);

                registro.Estado = "Enviado";
                registro.DetalleError = null;
                _context.EnviosCorreo.Add(registro);
                await _context.SaveChangesAsync(ct);

                return (true, null);
            }
            catch (OperationCanceledException)
            {
                registro.Estado = "Fallido";
                registro.DetalleError = "Tiempo de espera excedido o cancelado al enviar correo.";
                _context.EnviosCorreo.Add(registro);
                await _context.SaveChangesAsync(CancellationToken.None);
                return (false, "El envío de correo excedió el tiempo de espera.");
            }
            catch (Exception)
            {
                registro.Estado = "Fallido";
                registro.DetalleError = "Error al comunicarse con el servidor SMTP.";
                _context.EnviosCorreo.Add(registro);
                await _context.SaveChangesAsync(CancellationToken.None);
                return (false, "No se pudo enviar el correo electrónico.");
            }
        }
    }
}