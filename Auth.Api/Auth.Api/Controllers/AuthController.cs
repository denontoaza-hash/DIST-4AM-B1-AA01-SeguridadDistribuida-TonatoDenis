using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Auth.Api.DTOs;
using Auth.Api.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private async Task<string> GenerarJwtAsync(ApplicationUser user, IEnumerable<string>? scopesExtra = null)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
            var issuer = jwtSection["Issuer"] ?? "Auth.Api";
            var audience = jwtSection["Audience"] ?? "SistemaComercial.Api";
            var minutos = int.TryParse(jwtSection["ExpirationMinutes"], out var m) ? m : 60;

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new("nombres", user.Nombres),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var rol in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            if (scopesExtra != null)
            {
                foreach (var sc in scopesExtra)
                {
                    claims.Add(new Claim("scope", sc));
                }
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(minutos),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GenerarTokenServicioInternoAsync(string scope)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
            var issuer = jwtSection["Issuer"] ?? "Auth.Api";
            var audience = jwtSection["Audience"] ?? "SistemaComercial.Api";

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "auth-api-servicio"),
                new(ClaimTypes.NameIdentifier, "auth-api-servicio"),
                new("scope", scope),
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

        // POST: api/Auth/registro
        [HttpPost("registro")]
        [AllowAnonymous]
        public async Task<IActionResult> Registro([FromBody] RegistroRequestDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var correoNorm = dto.Correo.Trim().ToLowerInvariant();
            var existente = await _userManager.FindByEmailAsync(correoNorm);
            if (existente != null)
                return Conflict(new { mensaje = "El correo ya se encuentra registrado." });

            var user = new ApplicationUser
            {
                UserName = correoNorm,
                Email = correoNorm,
                Nombres = dto.Nombres.Trim(),
                EstaActivo = false, // Se habilita al completar perfil en Usuarios.Api
                FechaCreacion = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
                return BadRequest(new { errores = createResult.Errors.Select(e => e.Description) });

            if (!await _roleManager.RoleExistsAsync("Cliente"))
                await _roleManager.CreateAsync(new IdentityRole("Cliente"));

            await _userManager.AddToRoleAsync(user, "Cliente");

            // 1) Llamar a Usuarios.Api para crear el perfil mediante IHttpClientFactory
            try
            {
                var usuariosClient = _httpClientFactory.CreateClient("UsuariosApiClient");
                var internalToken = await GenerarTokenServicioInternoAsync("usuarios.internos");
                usuariosClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", internalToken);

                var perfilPayload = new
                {
                    UsuarioId = user.Id,
                    Nombres = user.Nombres,
                    Correo = user.Email,
                    Telefono = dto.Telefono,
                    Ciudad = dto.Ciudad
                };

                // Soporte para simular latencia/timeout (Caso de prueba 14)
                if (Request.Query.ContainsKey("simularTimeout") || Request.Headers.ContainsKey("X-Simulate-Delay"))
                {
                    usuariosClient.DefaultRequestHeaders.Add("X-Simulate-Delay", "15000");
                }

                var perfilResp = await usuariosClient.PostAsJsonAsync("api/Perfiles", perfilPayload, ct);
                if (!perfilResp.IsSuccessStatusCode)
                {
                    // Compensación: eliminar identidad creada para no dejar cuenta activa inconsistente
                    await _userManager.DeleteAsync(user);
                    return StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new { mensaje = "No se pudo completar el registro porque el servicio de perfiles no respondió correctamente." });
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                await _userManager.DeleteAsync(user);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { mensaje = "El registro excedió el tiempo de espera al contactar al servicio de perfiles." });
            }
            catch (HttpRequestException)
            {
                await _userManager.DeleteAsync(user);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { mensaje = "No se pudo contactar al servicio de perfiles; el registro no se completó." });
            }

            // Activar cuenta tras persistir el perfil en UsuariosDB
            user.EstaActivo = true;
            await _userManager.UpdateAsync(user);

            // 2) Solicitar a Notificaciones.Api correo de bienvenida (sin bloquear ni exponer secretos)
            try
            {
                var notifClient = _httpClientFactory.CreateClient("NotificacionesApiClient");
                var notifToken = await GenerarTokenServicioInternoAsync("notificaciones.internas");
                notifClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", notifToken);

                var correoPayload = new
                {
                    Destinatario = user.Email,
                    Asunto = "Bienvenido al sistema comercial",
                    TipoNotificacion = "Bienvenida",
                    CuerpoTexto = $"Hola {user.Nombres}, tu cuenta ha sido registrada con el rol Cliente."
                };

                await notifClient.PostAsJsonAsync("api/Notificaciones/enviar", correoPayload, ct);
            }
            catch
            {
                // El usuario ya quedó registrado y con perfil creado; no se expone detalle interno
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                mensaje = "Usuario registrado correctamente.",
                usuarioId = user.Id,
                correo = user.Email,
                rol = "Cliente"
            });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var correoNorm = dto.Correo.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(correoNorm);

            if (user == null)
                return Unauthorized(new { mensaje = "Credenciales inválidas." });

            if (!user.EstaActivo)
                return Unauthorized(new { mensaje = "La cuenta se encuentra deshabilitada." });

            if (await _userManager.IsLockedOutAsync(user))
                return StatusCode(StatusCodes.Status423Locked,
                    new { mensaje = "Cuenta bloqueada temporalmente por intentos fallidos." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return StatusCode(StatusCodes.Status423Locked,
                    new { mensaje = "Cuenta bloqueada temporalmente por múltiples intentos fallidos." });

            if (!result.Succeeded)
                return Unauthorized(new { mensaje = "Credenciales inválidas." });

            var token = await GenerarJwtAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                token,
                usuarioId = user.Id,
                correo = user.Email,
                nombres = user.Nombres,
                roles
            });
        }

        // POST: api/Auth/google
        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequestDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var clientId = _configuration["GoogleAuth:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { mensaje = "La autenticación con Google no está configurada en el servidor." });

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch
            {
                return Unauthorized(new { mensaje = "El token de identidad de Google no es válido." });
            }

            if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
                return Unauthorized(new { mensaje = "Token de Google incompleto." });

            // Buscar por identificador estable del proveedor (sub)
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.GoogleSubjectId == payload.Subject, ct);

            if (user == null)
            {
                var correoNorm = payload.Email.Trim().ToLowerInvariant();
                var cuentaConMismoCorreo = await _userManager.FindByEmailAsync(correoNorm);
                if (cuentaConMismoCorreo != null)
                {
                    return Conflict(new
                    {
                        mensaje = "Ya existe una cuenta con este correo. Inicie sesión localmente para vincular su cuenta."
                    });
                }

                user = new ApplicationUser
                {
                    UserName = correoNorm,
                    Email = correoNorm,
                    EmailConfirmed = payload.EmailVerified,
                    Nombres = payload.Name ?? correoNorm,
                    GoogleSubjectId = payload.Subject,
                    EstaActivo = false,
                    FechaCreacion = DateTime.UtcNow
                };

                var creado = await _userManager.CreateAsync(user);
                if (!creado.Succeeded)
                    return BadRequest(new { errores = creado.Errors.Select(e => e.Description) });

                if (!await _roleManager.RoleExistsAsync("Cliente"))
                    await _roleManager.CreateAsync(new IdentityRole("Cliente"));

                await _userManager.AddToRoleAsync(user, "Cliente");

                // Crear perfil en Usuarios.Api
                try
                {
                    var usuariosClient = _httpClientFactory.CreateClient("UsuariosApiClient");
                    var internalToken = await GenerarTokenServicioInternoAsync("usuarios.internos");
                    usuariosClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", internalToken);

                    var perfilResp = await usuariosClient.PostAsJsonAsync("api/Perfiles", new
                    {
                        UsuarioId = user.Id,
                        Nombres = user.Nombres,
                        Correo = user.Email
                    }, ct);

                    if (!perfilResp.IsSuccessStatusCode)
                    {
                        await _userManager.DeleteAsync(user);
                        return StatusCode(StatusCodes.Status503ServiceUnavailable,
                            new { mensaje = "No se pudo crear el perfil en Usuarios.Api." });
                    }
                }
                catch
                {
                    await _userManager.DeleteAsync(user);
                    return StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new { mensaje = "Servicio de perfiles no disponible para completar el acceso." });
                }

                user.EstaActivo = true;
                await _userManager.UpdateAsync(user);

                // Enviar correo de bienvenida al nuevo usuario de Google
                try
                {
                    var notifClient = _httpClientFactory.CreateClient("NotificacionesApiClient");
                    var notifToken = await GenerarTokenServicioInternoAsync("notificaciones.internas");
                    notifClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", notifToken);

                    var correoPayload = new
                    {
                        Destinatario = user.Email,
                        Asunto = "Bienvenido al sistema comercial",
                        TipoNotificacion = "Bienvenida",
                        CuerpoTexto = $"Hola {user.Nombres}, tu cuenta ha sido creada exitosamente mediante Google con el rol Cliente."
                    };

                    await notifClient.PostAsJsonAsync("api/Notificaciones/enviar", correoPayload, ct);
                }
                catch
                {
                    // No bloquea el acceso si falla la notificación
                }
            }

            if (!user.EstaActivo)
                return Unauthorized(new { mensaje = "La cuenta se encuentra deshabilitada." });

            var jwtLocal = await GenerarJwtAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                token = jwtLocal,
                usuarioId = user.Id,
                correo = user.Email,
                nombres = user.Nombres,
                roles
            });
        }

        // POST: api/Auth/recuperar-password
        [HttpPost("recuperar-password")]
        [AllowAnonymous]
        public async Task<IActionResult> RecuperarPassword([FromBody] RecuperarPasswordRequestDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var respuestaGenerica = Ok(new
            {
                mensaje = "Si el correo está registrado y activo, se enviarán instrucciones para restablecer la contraseña."
            });

            var correoNorm = dto.Correo.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(correoNorm);
            if (user == null || !user.EstaActivo)
                return respuestaGenerica;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var baseUrlReset = _configuration["Recuperacion:ResetUrlBase"] ?? "https://localhost:7101/reset-password.html";
            var enlace = $"{baseUrlReset}?email={Uri.EscapeDataString(correoNorm)}&token={Uri.EscapeDataString(token)}";

            try
            {
                var notifClient = _httpClientFactory.CreateClient("NotificacionesApiClient");
                var notifToken = await GenerarTokenServicioInternoAsync("notificaciones.internas");
                notifClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", notifToken);

                var resp = await notifClient.PostAsJsonAsync("api/Notificaciones/enviar", new
                {
                    Destinatario = correoNorm,
                    Asunto = "Recuperación de contraseña",
                    TipoNotificacion = "RecuperacionPassword",
                    CuerpoTexto = $"Utiliza este enlace para restablecer tu contraseña: {enlace}"
                }, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new { mensaje = "No se pudo procesar la solicitud de recuperación en este momento." });
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { mensaje = "La solicitud de recuperación excedió el tiempo de espera." });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { mensaje = "El servicio de notificaciones no está disponible en este momento." });
            }

            return respuestaGenerica;
        }

        // POST: api/Auth/restablecer-password
        [HttpPost("restablecer-password")]
        [AllowAnonymous]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var correoNorm = dto.Correo.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(correoNorm);
            if (user == null)
                return BadRequest(new { mensaje = "Solicitud de restablecimiento no válida o token expirado." });

            var resetResult = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NuevaPassword);
            if (!resetResult.Succeeded)
                return BadRequest(new { mensaje = "No se pudo restablecer la contraseña. El token es inválido, venció o ya fue utilizado." });

            return Ok(new { mensaje = "Contraseña actualizada correctamente." });
        }

        // POST: api/Auth/cambiar-password
        [HttpPost("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            var changeResult = await _userManager.ChangePasswordAsync(user, dto.PasswordActual, dto.NuevaPassword);
            if (!changeResult.Succeeded)
                return BadRequest(new { mensaje = "No se pudo cambiar la contraseña. Verifique la contraseña actual." });

            return Ok(new { mensaje = "Contraseña cambiada correctamente." });
        }

        // GET: api/Auth/usuarios
        [HttpGet("usuarios")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ListarUsuarios(CancellationToken ct)
        {
            var usuarios = await _userManager.Users
                .AsNoTracking()
                .ToListAsync(ct);

            var resultado = new List<object>();
            foreach (var u in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(u);
                resultado.Add(new
                {
                    u.Id,
                    u.Nombres,
                    u.Email,
                    u.EstaActivo,
                    u.FechaCreacion,
                    Roles = roles
                });
            }

            return Ok(resultado);
        }

        // PUT: api/Auth/usuarios/{id}/rol
        [HttpPut("usuarios/{id}/rol")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CambiarRol(string id, [FromBody] CambiarRolRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Rol != "Cliente" && dto.Rol != "Administrador")
                return BadRequest(new { mensaje = "El rol debe ser Cliente o Administrador." });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            var rolesActuales = await _userManager.GetRolesAsync(user);

            // Proteger al último administrador activo si se intenta quitar el rol Administrador
            if (rolesActuales.Contains("Administrador") && dto.Rol != "Administrador" && user.EstaActivo)
            {
                var adminsActivos = await _userManager.GetUsersInRoleAsync("Administrador");
                var cantidadActivos = adminsActivos.Count(a => a.EstaActivo);
                if (cantidadActivos <= 1)
                    return BadRequest(new { mensaje = "No se puede quitar el rol al último administrador activo." });
            }

            if (!await _roleManager.RoleExistsAsync(dto.Rol))
                await _roleManager.CreateAsync(new IdentityRole(dto.Rol));

            await _userManager.RemoveFromRolesAsync(user, rolesActuales);
            await _userManager.AddToRoleAsync(user, dto.Rol);

            return Ok(new { mensaje = "Rol actualizado correctamente.", usuarioId = user.Id, rol = dto.Rol });
        }

        // PUT: api/Auth/usuarios/{id}/estado
        [HttpPut("usuarios/{id}/estado")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CambiarEstado(string id, [FromBody] CambiarEstadoCuentaRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            // Proteger al último administrador activo si se intenta deshabilitar
            if (user.EstaActivo && !dto.EstaActivo && await _userManager.IsInRoleAsync(user, "Administrador"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrador");
                var activos = admins.Count(a => a.EstaActivo);
                if (activos <= 1)
                    return BadRequest(new { mensaje = "No se puede deshabilitar al último administrador activo." });
            }

            user.EstaActivo = dto.EstaActivo;
            await _userManager.UpdateAsync(user);

            return Ok(new { mensaje = "Estado de cuenta actualizado.", usuarioId = user.Id, estaActivo = user.EstaActivo });
        }
    }
}