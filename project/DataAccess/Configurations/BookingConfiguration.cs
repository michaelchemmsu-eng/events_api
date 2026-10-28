using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using project.Models;
namespace project.DataAccess.Configurations
{
    public class BookingConfiguration: IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder) 
        {
            builder.ToTable("Bookings");

            //первичный ключ - Id
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();

            builder.Property(b => b.Status).HasConversion<string>();

            //эта связь уже настроена в конфигурации сущности Event
            //builder.HasOne(b => b.Event).WithMany(e => e.Bookings).HasForeignKey(b => b.EventId);


        }
    }
}
