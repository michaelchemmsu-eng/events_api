using project.Models;

namespace project.Services
{
    public interface IBookingRepository
    {
        //получить список всех бронирований
        PaginatedResultBookings GetAllBookings(Guid? EventId, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);
        //получить бронь по id;
        BookingResponse GetBookingById(Guid bookingId);

        // обновить событие целиком;
        void UpdateBooking(Guid bookingId, BookingDto BookingDto);
        // удалить событие;
        void DeleteBooking(Guid bookingId);
        BookingResponse CreateBooking(BookingRequest bookingRequest);
    }
}
