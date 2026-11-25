using Microsoft.EntityFrameworkCore;
using Notificaciones.Api.Models;

namespace Notificaciones.Api.Data
{
    public class NotificacionesDbContext : DbContext
    {
        public NotificacionesDbContext(DbContextOptions<NotificacionesDbContext> options)
            : base(options)
        {
        }

        public DbSet<EnvioCorreo> EnviosCorreo => Set<EnvioCorreo>();
    }
}