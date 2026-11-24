using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using project.DataAccess;
using project.Models;
using project.Services;
using project.Services.EventSpecificator;
namespace project.Repositories
{
    public class BookingRepository: IBookingRepository
    {
        private readonly AppDbContext _context;
     
        public async Task RollBackBookingAsync(Guid bookingId, Guid eventId, CancellationToken ct = default) 
        {
           
                using var transaction = await _context.Database.BeginTransactionAsync(ct);


                
                await _context.Bookings
                    .Where(b => b.Id == bookingId)
                    .ExecuteUpdateAsync(setter => setter.SetProperty(b => b.Status, BookingStatus.Rejected), ct);
                await _context.Events.Where(e => e.Id == eventId)
                    .ExecuteUpdateAsync(setter => setter.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats + 1), ct);



                await transaction.CommitAsync(ct);
          

        }

        public BookingRepository(AppDbContext context)
        {
            _context = context;
 
        }
        public async Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Bookings.FindAsync(new object[] { id }, ct);
        }
        public async Task AddBookingAsync(Booking booking, CancellationToken ct = default)
        {
            await _context.Bookings.AddAsync(booking, ct);
            //await _context.SaveChangesAsync(ct);
        }
        public void UpdateBooking(Booking booking)
        {
            _context.Bookings.Update(booking);
           // await _context.SaveChangesAsync(ct);
        }
        public async Task DeleteBookingAsync(Booking booking, CancellationToken ct = default)
        {
            _context.Bookings.Remove(booking);
           // await _context.SaveChangesAsync(ct);
        }
        public async Task<List<Booking>> GetBookingsAsync(Guid eventId, CancellationToken ct = default)
        {
            return await _context.Bookings
                .Where(b => b.EventId == eventId)
                .ToListAsync(ct);
        }



        public async Task<List<Booking>> GetAllBookingsFilteredAsync(Specification<Booking> specification, CancellationToken ct = default)
        {
            var query = _context.Bookings.AsQueryable();
            query = SpecificationEvaluator.GetQuery(query, specification);
            return await query.ToListAsync(ct);
        }





        public async Task<List<Guid>> GetPendingBookingIds(CancellationToken ct = default) 
        {
            return await _context.Bookings.Where(b => b.Status == BookingStatus.Pending).Select(b => b.Id).ToListAsync(ct);
        }


        public async Task SaveChangesAsync(CancellationToken ct=default) 
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
