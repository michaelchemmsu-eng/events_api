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

        //options -- внешние настройки
        // регистрация контекста в Program.cs
        // внешние настройки передаются в конструктор контекста через DI
        //builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
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
            modelBuilder.Entity<Event>().Property(p => p.AvailableSeats).IsConcurrencyToken();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
