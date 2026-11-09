using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using project.DataAccess;
using project.Models;

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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingBackgroundService> _logger;
        private static readonly TimeSpan TimePause = TimeSpan.FromSeconds(30);

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
            List<Guid> pendingBookingIds;
            using (var scope = _scopeFactory.CreateScope()) 
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                pendingBookingIds = await context.Bookings
                            .Where(b => b.Status == BookingStatus.Pending)
                            .Select(b => b.Id)
                            .ToListAsync(ct);//запрос. может вылететь excpetion
            }
            if (!pendingBookingIds.Any())
                return;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 10, // Защита от перегрузки БД
                CancellationToken = ct
            };
            await Parallel.ForEachAsync(pendingBookingIds, parallelOptions, async (bookingId, ct) => 
            {
                //создаем scope и получаем appdbcontext
                using (var scope = _scopeFactory.CreateScope()) 
                {
                    
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var booking = await context.Bookings.
                        FirstOrDefaultAsync(b => b.Id == bookingId, ct);

                    if (booking is null)
                    {
                        return;
                    }

                    Guid currentEventId = booking.EventId;

                    try 
                    { 
                        bool eventExists = await context.Events.AnyAsync(e => e.Id == booking!.EventId, ct);
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
                        await context.SaveChangesAsync(ct);

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

                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                using var transaction = await context.Database.BeginTransactionAsync(ct);
                try
                {
                    await context.Bookings
                        .Where(b => b.Id == bookingId)
                        .ExecuteUpdateAsync(setter => setter.SetProperty(b => b.Status, BookingStatus.Rejected), ct);
                    await context.Events.Where(e => e.Id == eventId)
                        .ExecuteUpdateAsync(setter => setter.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats + 1), ct);
                    await transaction.CommitAsync(ct);
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





