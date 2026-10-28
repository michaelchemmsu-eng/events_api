using project.Models;

namespace project.Services
{
    public class BookingBackgroundService (
        IBookingRepository _bookingRepository,
        IEventService _eventRepository, //для проверки существования события во время обработки бронирования этого события 
        ILogger<BookingBackgroundService> _logger): BackgroundService
    {

        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);//Интервал опроса хранилища
        private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);//Задержка обработки одной брони


        //для защиты записи в хранилище
        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    //await Task.Delay(ProcessingDelay, stoppingToken);
                    var pendingBookings = _bookingRepository.GetPending().ToList();
                    var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                    await Task.WhenAll(tasks);
                    await Task.Delay(PollingInterval, stoppingToken);
                }
            }
            finally 
            {
                _logger.LogInformation("Сервис завершает работу, освобождаем ресурсы");
            }
        }


        //асинхронный метод  обработки одной брони
        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken) 
        {
            //защитить обновление хранилища.
            // добавить логику отклонения брони — если событие было удалено к моменту обработки
            // или произошла непредвиденная ошибка, бронь должна перейти в статус Rejected,
            // а место — вернуться в пул.
            _logger.LogInformation($"Процесс обработки бронирования {booking.Id}");


            
            if (stoppingToken.IsCancellationRequested)
                return;

            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            //проверка события по id в репозитории -- это обращение к БД. 
            try
            {
                bool isEventExist = await _eventRepository.IsExistInRepositoryAsync(booking.EventId, stoppingToken);
                if (isEventExist)
                {
                    booking.Confirm();
                    _logger.LogInformation("Бронирование {BookingId} подтверждено", booking.Id);
                }
                else
                {
                    // события нет на момент обработки бронирования
                    // событие в пул не возвращаем
                    booking.Reject();
                    _logger.LogWarning("Бронирование {BookingId} отклонено: событие не найдено", booking.Id);

                }
                //обновляем статус брони на Confirm или Reject из-за отсутствия события
                //сохраняем статус реджектед для 
                var BookingDto = new BookingDto
                {
                    Status = booking.Status,
                    ProcessedAt = booking.ProcessedAt
                };
                try
                {
                    _bookingRepository.UpdateBooking(booking.Id, BookingDto);

                }

                catch (Exception updateException)
                {
                    _logger.LogError(updateException, "Не удалось обновить статус брони {BookingId}", booking.Id);
                }


            }


            //в случае OperationCanceledException -- без reject() и отката
            catch (OperationCanceledException ex) 
            {
                _logger.LogInformation($"обработка бронирования {booking.Id} отменена");
                throw;
            }


            //другие ошибки приводят к reject() и откату бронировния
            catch (Exception ex)
            {

                booking.Reject();

                 _logger.LogError(ex, "Ошибка при обработке брони {BookingId}. Откат события и обновление бронирования...", booking.Id); 

                

                //откат. возвращаем место в пул:
                try
                {
                    await _eventRepository.ReleaseSeatAsync(booking.EventId); //
                }
                catch (Exception rollBackException)
                {
                    switch( rollBackException )
                    {
                        case KeyNotFoundException:
                            _logger.LogError(rollBackException, "Событие не удалось вернуть. ошибка получения события {EventId}", booking.EventId);
                            break;
                        default:
                            _logger.LogError(rollBackException, "Событие {EventId} не удалось вернуть ", booking.EventId);
                            break;
                    }   
                }

                //сохраняем статус реджектед для 
                var BookingDto = new BookingDto
                {
                    Status = booking.Status,
                    ProcessedAt = booking.ProcessedAt
                };
                try
                {
                    _bookingRepository.UpdateBooking(booking.Id, BookingDto);

                }

                catch (Exception updateException)
                {
                    _logger.LogError(updateException, "Не удалось обновить статус брони {BookingId}", booking.Id);
                }


            }
            finally
            {
                
                 _processingSemaphore.Release();

            }

        }
    }
}
