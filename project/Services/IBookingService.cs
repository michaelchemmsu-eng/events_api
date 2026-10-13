using project.Models;

namespace project.Services
{
    public interface IBookingService
    {
        //создает Booking для указанного события с идентификатором eventId
        Task<BookingResponse> CreateBookingAsync(Guid eventId);
        //получение брони по идентификатору.
        Task<BookingResponse> GetBookingByIdAsync(Guid bookingId);
    }
}
