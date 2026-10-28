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
        /// Получить все бронирования в виде массива BookingsResponse[], пустого или непустого
        /// 
        /// </summary>
        /// <returns> BookingsResponse[] </returns>
        BookingResponse[] GetAllBookings();

        //получить бронь по id;
        /// <summary>
        /// Получить бронирование по его уникальному идентификатору (bookingId).
        /// выбросит исключение, если бронирование с указанным идентификатором не найдено.
        /// </summary>
        /// <param name="bookingId"></param>
        /// <returns></returns>
        BookingResponse GetBookingById(Guid bookingId);

        // обновить событие целиком;
        /// <summary>
        /// Обновить существующее бронирование с указанным уникальным идентификатором (bookingId) на основе предоставленных данных в объекте BookingDto.
        /// выбрасывает исключение, если бронирование с указанным идентификатором не найдено.
        /// </summary>
        /// <param name="bookingId"></param>
        /// <param name="BookingDto"></param>
        void UpdateBooking(Guid bookingId, BookingDto BookingDto);
        // удалить событие;
        /// <summary>
        /// Удалить существующее бронирование с указанным уникальным идентификатором (bookingId).
        /// выбрасывает исключение, если бронирование с указанным идентификатором не найдено.
        /// </summary>
        /// <param name="bookingId"></param>
        void DeleteBooking(Guid bookingId);
        /// <summary>
        /// Создать новое бронирование на основе предоставленных данных в объекте BookingRequest.
        /// вернуть BookingResponse с информацией о созданном бронировании.
        /// </summary>
        /// <param name="bookingRequest"></param>
        /// <returns>BookingResponse с информацией о созданном бронировании</returns>
        BookingResponse CreateBooking(BookingRequest bookingRequest);

        /// <summary>
        /// Возвращает из хранилища объект IEnumerable с элементами Booking
        /// которые имеют статус Pending
        /// </summary>
        /// <returns>IEnumerable объектов Booking</returns>
        IEnumerable<Booking> GetPending();


    }
}
