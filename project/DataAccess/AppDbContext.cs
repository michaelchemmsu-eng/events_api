using Microsoft.EntityFrameworkCore;

using project.Models;

namespace project.DataAccess
{
    public class AppDbContext: DbContext
    {
        /// <summary>
        /// Таблица Events
        /// </summary>
        public DbSet<Event> Events { get; set; }
        /// <summary>
        /// Таблица Bookings
        /// </summary>
        public DbSet<Booking> Bookings { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        /// <summary>
        /// подключить все конфигурации к контексту
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
