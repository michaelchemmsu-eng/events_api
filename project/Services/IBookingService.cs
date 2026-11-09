using project.Models;
using project.CustomExceptions;

namespace project.Services
{
    public interface IBookingService
    {
        //создает Booking для указанного события с идентификатором eventId
        /// <summary>
        /// Резервирование одного места для события. Id события передается в аргуемент метода.
        /// выбрасывает исключение KeyNotFoundException в случае отсутсвия события с переданным Id.
        /// Выбрасывает исключение NoAvailableSeatsException, если нет доступных мест для резервирования
        /// </summary>
        /// <param name="eventId"> Id события</param>
        /// <param name="ct"> токен отмены</param>
        /// <returns> BookingResponse </returns>
        /// <exception cref="NoAvailableSeatsException"></exception>
        Task<BookingResponse> CreateBookingAsync(Guid eventId, CancellationToken ct=default);
        //получение брони по идентификатору.
        /// <summary>
        /// Асинхронный метод.
        /// метод возвращает информацию о бронировании по идентификатору бронирования.
        /// Генерирует исключение, если в БД нет информации о бронировании с переданным идентификатором,
        /// если пользователь отменил запрос, или произошла ошибка на стороне БД.
        /// </summary>
        /// <param name="bookingId">идентификатор бронирования</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        Task<BookingResponse> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
    }
}
