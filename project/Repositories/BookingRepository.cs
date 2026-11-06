using project.Models;
using project.DataAccess;
using Microsoft.EntityFrameworkCore;
namespace project.Repositories
{
    public class BookingRepository: IBookingRepository
    {
        private readonly AppDbContext _context;
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
        public async Task UpdateBookingAsync(Booking booking, CancellationToken ct = default)
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

        public async Task SaveChangesAsync(CancellationToken ct=default) 
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
