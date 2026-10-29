using project.CustomExceptions;
using project.Models;
using project.DataAccess;
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
        private readonly object _bookingLock = new(); // для защиты критической секции
        private readonly AppDbContext _context;
        public BookingService(IBookingRepository BookingStorage, ILogger<BookingService> Logger, IEventService EventService, AppDbContext context) 
        {
            _bookingStorage = BookingStorage;
            _logger = Logger;
            _eventService = EventService;
            _context = context;
        }

        /// <summary>
        /// Резервирование одного места для события. Id события передается в аргуемент метода.
        /// выбрасывает исключение KeyNotFoundException в случае отсутсвия события с переданным Id.
        /// Выбрасывает исключение NoAvailableSeatsException, если нет доступных мест для резервирования
        /// </summary>
        /// <param name="eventId"> Id события</param>
        /// <returns> BookingResponse </returns>
        /// <exception cref="NoAvailableSeatsException"></exception>
        public async Task<BookingResponse> CreateBookingAsync(Guid eventId, CancellationToken ct) 
        {
            /*
             * поступает запрос из нескольких потоков на один и тот же eventId
             * обращаемся к усулге ивент сервис 
             * 
             */
           
            try
            {
                //EventService хранит тот же экземпляр AppDbContext , как так Scope.
                await _eventService.TryReserveSeatsAsync(eventId,ct);//либо успешно резервирует, либо бросает исключение

                var NewBookingObj = Booking.Create(new BookingRequest {EventId = eventId});
                var savedBookingEntityEntry = _context.Bookings.Add(NewBookingObj);
                await _context.SaveChangesAsync(ct);
                return savedBookingEntityEntry.Entity.ToBookingResponse();
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (NoAvailableSeatsException ex) 
            {
                _logger.LogInformation(ex, "Ошибка при создании бронирования для события {EventId}", eventId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Ошибка при создании бронирования для события {EventId}", eventId);
                throw;
            }
            
            

        }

        public async Task<BookingResponse> GetBookingByIdAsync(Guid bookingId) 
        {
            try 
            {
               
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
