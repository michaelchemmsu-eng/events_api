using project.Models;
using project.DataAccess;
using Microsoft.EntityFrameworkCore;
using project.Services.EventSpecificator;
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
