using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.CustomExceptions;
using project.DataAccess;
using project.Models;
using project.Repositories;
using project.Services.Specifications;
using System.ComponentModel.DataAnnotations;
namespace project.Services
{

    public class BookingService: IBookingService
    {
 
        private readonly ILogger<BookingService> _logger;
        private readonly IEventService _eventService;
        private readonly IBookingRepository _bookingRepository;

       
        public BookingService(ILogger<BookingService> Logger, IEventService EventService, IBookingRepository BookingRepository) 
        {

            _logger = Logger;
            _eventService = EventService;
            _bookingRepository = BookingRepository;
           
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
             
            try
            {
            
                var NewBookingObj = Booking.Create(new BookingRequest { EventId = eventId });
                await _bookingRepository.AddBookingAsync(NewBookingObj, ct);
                await _eventService.ReserveSeatsAsync(eventId, ct);//либо успешно резервирует, либо бросает исключение
                await _bookingRepository.SaveChangesAsync();
                return NewBookingObj.ToBookingResponse();
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
                var existingBooking = await _bookingRepository.GetBookingByIdAsync(bookingId, ct);
                if (existingBooking is not null)
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


        public async Task<List<Booking>> GetAllBookingsAsync(
           string? title,
           DateTime? from,
           DateTime? to,
           int page,
           int pageSize,
           CancellationToken ct = default)
        {
            //создаем спецификацию для фильтрации
            var spec = new BookingSearchSpecification(title, from, to, page, pageSize);
            return await _bookingRepository.GetAllBookingsFilteredAsync(spec, ct);
        }
    }
}
