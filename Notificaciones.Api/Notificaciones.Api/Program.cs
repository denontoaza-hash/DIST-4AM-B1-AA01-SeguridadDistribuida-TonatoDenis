using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Notificaciones.Api.Data;
using Notificaciones.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Persistencia independiente: NotificacionesDB
builder.Services.AddDbContext<NotificacionesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("NotificacionesConnection")));

// Servicio de envío real de correos y registro en EnviosCorreo
builder.Services.AddScoped<ICorreoService, CorreoSmtpService>();

// IHttpClientFactory con Timeout explícito (por si se usa API de proveedor de correo)
builder.Services.AddHttpClient("CorreoProveedorClient", client =>
{
    var baseUrl = builder.Configuration["CorreoProveedor:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(10);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Validación JWT para proteger endpoints internos de notificaciones
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key en User Secrets o entorno.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(15)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();