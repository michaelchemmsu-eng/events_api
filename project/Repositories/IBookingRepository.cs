using project.Models;
namespace project.Repositories
{
    public interface IBookingRepository
    {
        //CRUD
        Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default);
        Task AddBookingAsync(Booking booking, CancellationToken ct = default);
        Task UpdateBookingAsync(Booking booking, CancellationToken ct = default);
        Task DeleteBookingAsync(Booking booking, CancellationToken ct = default);
        Task <List<Booking>> GetBookingsAsync(Guid eventId, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
