using project.CustomExceptions;
using project.Models;
using project.DataAccess;
using Microsoft.EntityFrameworkCore;
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
        //private readonly IBookingRepository _bookingStorage;
        private readonly ILogger<BookingService> _logger;
        private readonly IEventService _eventService;
      
        private readonly AppDbContext _context;
        public BookingService(IBookingRepository BookingStorage, ILogger<BookingService> Logger, IEventService EventService, AppDbContext context) 
        {
            //_bookingStorage = BookingStorage;
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
        /// <param name="ct"> токен отмены</param>
        /// <returns> BookingResponse </returns>
        /// <exception cref="NoAvailableSeatsException"></exception>
        public async Task<BookingResponse> CreateBookingAsync(Guid eventId, CancellationToken ct = default) 
        {
            /*
             * поступает запрос из нескольких потоков на один и тот же eventId
             * обращаемся к усулге ивент сервис 
             * 
             */
            
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            // проблема Потерянного билета
            //  так как ниже по коду TryReserveSeatsAsync делает запрос в БД в обход ChangeTracker
            // а SaveChangesAsync может завершится с генерацией исключения, не сохранив объект бронирования
            // в БД, то обе операции TryReserveSeatsAsync
            // и SaveChangesAsync нужно выполнить в одной транзакции
            try
            {
                //EventService хранит тот же экземпляр AppDbContext , как так Scope.
                await _eventService.TryReserveSeatsAsync(eventId,ct);//либо успешно резервирует, либо бросает исключение

                var NewBookingObj = Booking.Create(new BookingRequest {EventId = eventId});
                var savedBookingEntityEntry = _context.Bookings.Add(NewBookingObj);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);//завершаем транзакцию
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
                _logger.LogError(ex, "Ошибка при создании бронирования для события {EventId}", eventId);
                throw;
            }


        }

        /// <summary>
        /// Асинхронный метод.
        /// метод возвращает информацию о бронировании по идентификатору бронирования.
        /// Генерирует исключение, если в БД нет информации о бронировании с переданным идентификатором,
        /// если пользователь отменил запрос, или произошла ошибка на стороне БД.
        /// </summary>
        /// <param name="bookingId">идентификатор бронирования</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        public async Task<BookingResponse> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default) 
        {
            try
            {
                var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
                if (existingBooking is null)
                    return existingBooking!.ToBookingResponse();
                else 
                    throw new KeyNotFoundException($"Бронирование с идентификатором {bookingId} не найдено");
             
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Бронирование с идентификатором {BookingId} не найдено", bookingId);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Операция была отменена пользователем");
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
