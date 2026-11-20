using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using project.DataAccess;
using project.Models;
using project.Repositories;
using project.Services.Specifications;

namespace project.Services
{
    /*
     * фоновый сервис - singletone. AppDbContext - Scoped. Фоновый сервис создает Scope, в нем достать AppDbContext,
     * провести манипуляции с объектами Booking из БД, уничтожить AppDbContext при закрытии Scope.
     * получает периодически из БД все Bookings , которые имеют статус pending.
     * обновляет их статус на confirmed , если событие существует в БД.
     * обновляет статус на rejected, если события нет в БД. 
     * сохранить объект бронирования с новым статусом в БД.
     * если приложение закрылось,OperationCancelledExceptio, то без reject и отката (это возврат места одного места в событие)
     * другие ошибки приводят к reject и откату бронирования.
     */
    public class BookingBackgroundService: BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory; // производитель скоупов.
        private readonly ILogger<BookingBackgroundService> _logger;
        private static readonly TimeSpan TimePause = TimeSpan.FromSeconds(30);// время задержки перед следующей итерацией цикла в методе ExecuteAsync

        public BookingBackgroundService (IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logge )
        {
            _scopeFactory = scopeFactory;
            _logger= logge;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновый воркер обработки бронирований запущен.");
            while (!stoppingToken.IsCancellationRequested) 
            {
                try
                {
                    await ProcessBookingsAsync(stoppingToken);//

                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Фоновый воркер штатно завершает свою работу.");
                    break;
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "Глобальный сбой при обработке бронирований. Повторная попытка через {Delay} сек.", TimePause.TotalSeconds);
                }
                await Task.Delay(TimePause, stoppingToken);

            }


        }



        //асинхронный метод обработки всех бронирований 
        /*
        допущение: приложение работает в единственном экземпляре на сервере, асинхронно обрабатывает запросы клиентов
        CreateBooking создает объект бронирования в БД со статусом pending, если на момент создания есть места на ивент
        Сейчас в БД хранятся объекты букинг со статусом пендинг
        Фоновый сервис обрабатывает объекты букинг со статусов пендинг

         */
        private async Task ProcessBookingsAsync(CancellationToken ct)
        {
            //получаем все pending бронирования из БД
            List<Guid> pendingBookingIds = new List<Guid>();
            using (var scope = _scopeFactory.CreateScope()) 
            {
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        
                pendingBookingIds = await bookingRepository.GetPendingBookingIds(ct);
            }
            if (!pendingBookingIds.Any())
                return;


            //параллельная обработка всех pending бронирований
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 10, // Защита от перегрузки БД
                CancellationToken = ct
            };
            await Parallel.ForEachAsync(pendingBookingIds, parallelOptions, async (bookingId, ct) => 
            {
                //создаем scope и получаем booking repository
                using (var scope = _scopeFactory.CreateScope()) 
                {

                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                    var booking = await bookingRepository.GetBookingByIdAsync(bookingId, ct);

                    if (booking is null)
                    {
                        return;
                    }

                    Guid currentEventId = booking.EventId;

                    try 
                    {
                        //сломалась одна конкретная бронь, то мы логируем ошибку и откатываем изменения по этой брони,
                        //но не прерываем обработку других бронирований
                        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                        bool eventExists = await eventRepository.IsExistInRepositoryAsync(booking!.EventId, ct);
                        if (eventExists)
                        {
                            booking!.Confirm();
                        }
                        else
                        {
                            //потому что не существует в бд 
                            booking!.Reject();
                        }
                        //здесь другой поток потенциально может удалить Event. Но в БД Booking имеет FK ограничение, поэтому
                        // либо Event не удалится, либо сработает каскадное удаление -- удалится booking, который ссылается на этот удаляющийся Event
                        //допустим сработало каскадное удаление и объекта booking, который сейчас хранится в памяти, нет в БД
                        // UPDATE Bookings Set Status = BookingsStatus.Confirmed Where Id = bookingId и EF Core выбросит ошибку
                        await bookingRepository.SaveChangesAsync(ct);

                    }
                    catch (OperationCanceledException)
                    {

                        throw;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        _logger.LogWarning("Бронирование {BookingId} было удалено другим процессом до подтверждения.", bookingId);
                        //исключение не пробрасывается
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Ошибка во время обработки бронирования с Id {BookingId}. Откат бронирования...", bookingId);
                        await RollBackBookingAsync(bookingId, currentEventId, ct);
                    }

                }


            } ); 
            
        }

        //метод для отката
        private async Task RollBackBookingAsync(Guid bookingId, Guid eventId, CancellationToken ct)
        {
            //нужен объект контекста, но он scoped. Поэтому создает еще один Scope
            using (var scope = _scopeFactory.CreateScope())
            {

                try
                {
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    await bookingRepository.RollBackBookingAsync(bookingId, eventId, ct);
                }
                catch (OperationCanceledException)
                {

                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Не удалось откатить бронирование {BookingId}", bookingId);
                    //throw;
                }

            }

        }

    }
}





