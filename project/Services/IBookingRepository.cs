using project.Models;

namespace project.Services
{
    /// <summary>
    /// интерфейс репозитория для управления бронированиями.
    /// </summary>
    public interface IBookingRepository
    {
        //получить список всех бронирований
        /// <summary>
        /// Получить все бронирования с возможностью фильтрации по EventId и диапазону дат, а также с поддержкой пагинации.
        /// принимает следующие параметры:
        /// Guid? GuidEventId - фильтрация по EventId. Если null, то фильтрация не применяется.
        /// DateTime? from - начальная дата. Если null, то фильтрация не применяется.
        /// DateTime? to - конечная дата. Если null, то фильтрация не применяется.
        /// int page - номер страницы. По умолчанию 1.
        /// int pageSize - количество элементов на странице. По умолчанию 10.
        /// </summary>
        /// <param name="EventId">фильтрация по EventId. Если null, то фильтрация не применяется.</param>
        /// <param name="from">начальная дата. Если null, то фильтрация не применяется.</param>
        /// <param name="to">конечная дата. Если null, то фильтрация не применяется.</param>
        /// <param name="page">номер страницы. По умолчанию 1.</param>
        /// <param name="pageSize">количество элементов на странице. По умолчанию 10.</param>
        /// <returns></returns>
        PaginatedResultBookings GetAllBookingsPaginated(Guid? EventId, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);

        /// <summary>
        /// Получить все бронирования в виде массива BookingsResponse[]
        /// </summary>
        /// <returns> BookingsResponse[]</returns>
        BookingResponse[] GetAllBookings(); 

        //получить бронь по id;
        BookingResponse GetBookingById(Guid bookingId);

        // обновить событие целиком;
        void UpdateBooking(Guid bookingId, BookingDto BookingDto);
        // удалить событие;
        void DeleteBooking(Guid bookingId);
        BookingResponse CreateBooking(BookingRequest bookingRequest);


    }
}
