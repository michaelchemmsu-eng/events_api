using project.Models;
using project.Services.EventSpecificator;
namespace project.Repositories
{
    public interface IBookingRepository
    {
        //CRUD
        Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default);
        Task AddBookingAsync(Booking booking, CancellationToken ct = default);
        void UpdateBooking(Booking booking);
        Task DeleteBookingAsync(Booking booking, CancellationToken ct = default);
        Task <List<Booking>> GetBookingsAsync(Guid eventId, CancellationToken ct = default);
        Task<List<Guid>> GetPendingBookingIds(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task<List<Booking>> GetAllBookingsFilteredAsync(Specification<Booking> specification, CancellationToken ct = default);
    }
}
