using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using project.Models;

namespace project.DataAccess.Configurations
{
    /// <summary>
    /// Конфигурация сущности Event. Описание mapping
    /// </summary>
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(EntityTypeBuilder<Event> builder) 
        {
            builder.ToTable("Events");

            //первичный ключ - Id
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            //Title - обязательное поле
            builder.Property(e => e.Title).IsRequired().HasMaxLength(200);

            builder.Property(e => e.Description).IsRequired().HasMaxLength(2000);

            //StartAt - обязательное
            builder.Property(e => e.StartAt).IsRequired();

            //EndAt - обязательное
            builder.Property(e => e.EndAt).IsRequired();

            //TotalSeats - обязательное
            builder.Property(e => e.TotalSeats).IsRequired();


            //свять Events 1-->N Bookings
            builder.HasMany(e => e.Bookings).WithOne(b => b.Event).HasForeignKey(b => b.EventId);


        }
    }
}
