using project.Models;

namespace project.Services
{
    /*
     BookingService (Сервис бронирования):

    Генерирует Id = Guid.NewGuid().
    Ставит Status = BookingStatus.Pending.
    Ставит CreatedAt = DateTime.UtcNow.
    КЛАДЕТ бронь в хранилище (через репозиторий).
    И сразу отвечает клиенту статус Pending.

     */
    public class BookingService: IBookingService
    {
        private readonly IBookingRepository _bookingStorage;
        private readonly ILogger<BookingService> _logger;
        private readonly IEventService _eventService;
        public BookingService(IBookingRepository BookingStorage, ILogger<BookingService> Logger, IEventService EventService) 
        {
            _bookingStorage = BookingStorage;
            _logger = Logger;
            _eventService = EventService;
        }
        public async Task<BookingResponse> CreateBookingAsync(Guid eventId) 
        {
            try 
            {
                _eventService.GetEventById(eventId);// проверяем, что событие существует, если нет, то выбрасывается исключение KeyNotFoundException
                //создаем объект бронирования через репозиторий
                await Task.Delay(100); // имитация асинхронной операции
                return _bookingStorage.CreateBooking(new BookingRequest { EventId = eventId });
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании бронирования для события {EventId}", eventId);
                throw;
            }



        }

        public async Task<BookingResponse> GetBookingByIdAsync(Guid bookingId) 
        {
            try 
            {
                await Task.Delay(100);
                return _bookingStorage.GetBookingById(bookingId);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Бронирование с идентификатором {BookingId} не найдено", bookingId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении бронирования с идентификатором {BookingId}", bookingId);
                throw;
            }

        }
    }
}
