
using project.CustomExceptions;
using project.DataAccess;

using project.Models;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace project.Services
{
    public class EventService:IEventService
    {
        private ConcurrentDictionary<Guid,Event> _events = new();
        //словарь семафоров по eventId
        private ConcurrentDictionary<Guid, SemaphoreSlim> _semaphoreOfEventId = new();
        // алгоритм такой:
        /*
         * для Букинг сервиса 
         * несколько потоков отправляют запрос на создание букинга по eventId
         * все потоки обращаются к сервису событий на резервирование одного билета на один и тот же eventId
         *  и все эти потоки вызывают функцию TryReserveAsync (eventId)
         *  поток в методе TryReserveAsync (eventId): 
         *  захватывает семафор по eventId, проверяет есть ли это событие вообще в хранилище событий
         *  если нет, то бросить исключение
         *  если есть, то получает это событие Event, вызывает TryReserveSeats
         *  если мест не осталось, то бросаем исключение, если есть то минус одно место в AvailableSeats в объекте
         *  обновить событие в хранилище событий 
         */

        private AppDbContext _context;

        public EventService(AppDbContext context) 
        {
            _context = context;
        }


        public async Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct) 
        {
            bool IsExist = await _context.Events.AnyAsync(e => e.Id == id, ct);
            return IsExist;
        }


        ////метод для фонового процесса. метод отката
        //public async Task ReleaseSeatAsync(Guid eventId) 
        //{
        //    var sempahore = _semaphoreOfEventId.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        //    await sempahore.WaitAsync();

        //    try
        //    {
        //        if (!_events.TryGetValue(eventId, out Event? existingEvent))
        //        {
        //            throw new KeyNotFoundException($"Событие с идентификатором {eventId} не найдено.");
        //        }
        //        existingEvent.ReleaseSeats();
        //        //одновить событие в хранилище
        //        _events[eventId] = existingEvent;

        //    }
        //    catch (Exception) 
        //    {
        //        throw;
        //    }
        //    finally
        //    {
        //        sempahore.Release();
        //    }


        //}




        /// <summary>
        /// Попытка резервировать место для события
        /// </summary>
        /// <param name="eventId">идетификатор события, для которого резервируется 1 место</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NoAvailableSeatsException"></exception>
        public async Task TryReserveSeatsAsync(Guid eventId,CancellationToken ct=default) 
        {
            //первый вариант
            //var updatedRows = await _context.Events
            //    .Where(e => e.Id == eventId && e.AvailableSeats > 0)
            //    .ExecuteUpdateAsync(setter => setter.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - 1)
            //                            ,ct);
            //if (updatedRows > 0)
            //    return;
            ////выяснсяем по какой причине updatedRows =0
            ////до следующей строки другой поток может добавить событие, но маловероятно, что оно при добавлении будет c Id=eventId
            //bool eventExists = await _context.Events.AnyAsync(e => e.Id == eventId, ct);
            //if (!eventExists)
            //{
            //    throw new KeyNotFoundException($"Событие {eventId} не найдено.");
            //}
            //else 
            //{
            //    throw new NoAvailableSeatsException($"Нет доступных мест для события {eventId}");
            //}
            //второй вариант - оптимистичная блокировка
            int retryCount = 0;
            bool isSaved = false;
            var rowToUpdate = await _context.Events.Where(e => e.Id == eventId).FirstOrDefaultAsync();
            if (rowToUpdate is null)
            {
                throw new KeyNotFoundException($"Событие {eventId} не найдено.");
            }
            if (rowToUpdate.AvailableSeats == 0)
            {
                throw new NoAvailableSeatsException($"Нет доступных мест для события {eventId}");
            }
            while (!isSaved && retryCount < 5) 
            {
               
                try
                {
                    // Attempt to save changes to the database
                    await _context.SaveChangesAsync();
                    isSaved = true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    /*
                    нужно дойти до свойства AvailableSeats сущности, которая вызвала конфликт
                    посмотреть на текущее значение этого свойства в БД
                    если оно 0, то бросить исключение
                    если оно >0, то обновить Original Value в соответсвии с DataBase value
                    AvailbaleSeats обновить в соответсвии с DataBase value
                    повторить попытку обновления на Current Value.
                     */
                    //получаем объект entity , который вызвал конфликт
                    var entry = ex.Entries[0]; //Gets the entries that were involved in the error. Typically this is a single entry, but in some cases it may be zero or multiple entries.
                    //достаем из БД данные
                    var databaseValues = await entry.GetDatabaseValuesAsync(); //Queries the database for copies of the values of the tracked entity as they currently exist in the database
                    
                    if (databaseValues is null)
                    {
                        throw new KeyNotFoundException($"Событие {eventId} было удалено другим пользователем.");
                    }

                    int currentDbSeats = (int)databaseValues[nameof(Event.AvailableSeats)]!;

                    if (currentDbSeats <= 0)
                    {
                        throw new NoAvailableSeatsException($"Нет доступных мест для события {eventId}");
                    }

                    rowToUpdate.AvailableSeats = currentDbSeats - 1;
                    entry.OriginalValues.SetValues(databaseValues);//Original values are the values that were originally retrieved from the database, before any edits were made.
                }

            }
            if (!isSaved)
            {
                throw new Exception("Не удалось забронировать место из-за высокой нагрузки.");
            }

        }


        /// <summary>
        /// Асинхронный метод.
        /// Возвращает все строки в таблице Events. Результат будет отфильтрован
        /// по первым трем переданным в аргументах значениям,
        /// а результат пагинарован.
        /// </summary>
        /// <param name="title">параметр фильтрации по заголовку</param>
        /// <param name="from">параметр фильтрации по дате начала события</param>
        /// <param name="to">параметр фильтрации по дате окончания события</param>
        /// <param name="page">номер страницы результата</param>
        /// <param name="pageSize">размер страницы</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public async Task<PaginatedResult> GetAllEvents(
            string? title,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct=default
            )
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Номер страницы должен быть не меньше 1.");
            }
            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Размер страницы должен быть от 1 до 100.");
            }

            var query = _context.Events.AsQueryable();

           
            
            //фильтруем по title
            if (!string.IsNullOrEmpty(title))
            {
                // регистронезависимый поиск подстроки в заголовке
                query = query.Where(e => e.Title.Contains(title));
            }
            if (from.HasValue) 
            {
                query = query.Where(e => e.StartAt >= from.Value);
            }
            if (to.HasValue) 
            {
                query = query.Where(e => e.EndAt <= to.Value);
            }
            int totalCount = await query.CountAsync(ct);

            var pagedEvents = await query
                        .OrderBy(e => e.StartAt)//для тестирования сортируем по StartAt, так как ConcurrentDictionary не гарантирует порядок элементов. В тесте ожидаемые элементы хранятся в List.
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(ev => ev.ToEventResponse())
                        .ToArrayAsync();

            return new PaginatedResult
            {
                totalEvents = totalCount,
                eventArray = pagedEvents,
                currentPage = page,
                pageSizeOfCurrentPage = pagedEvents.Length
            };
            
        }


        /// <summary>
        /// асинхронный метод.
        /// Возвращает сущность EventResponse, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище 
        /// </summary>
        /// <param name="id">Id события Event</param>
        /// <param name="ct">токен отмены</param>
        /// <returns>EventResponse or throws KeyNotFoundException </returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров -- возвращает Dto
        public async Task<EventResponse> GetEventResponseById(Guid id, CancellationToken ct= default)
        {
            var EventObj = await _context.Events.FirstOrDefaultAsync(e=>e.Id == id,ct);
            if (EventObj is null)
            {
                throw new KeyNotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }
            return EventObj.ToEventResponse();
        }


        /// <summary>
        /// Асинхронный метод.
        /// Возвращает сущность Event, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище
        /// </summary>
        /// <param name="id">идентификатор сущности</param>
        /// <param name="ct">токен отмены</param>
        /// <returns>Event or throws KeyNotFoundException</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах -- возвращает Event
        public async Task <Event> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            var EventObj = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (EventObj is null)
            {
                throw new KeyNotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }
            return EventObj;
        }


        /// <summary>
        /// метод создания сущности Event.
        /// </summary>
        /// <param name="eventDto">Dto для создания новой сущности</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        public async Task <EventResponse> CreateEventAsync(EventDto eventDto, CancellationToken ct = default) 
        {
            var newEvent = Event.Create(eventDto);//бросает исключение, если eventDto.TotalSeats!.Value < 0
            _context.Add(newEvent);
            await _context.SaveChangesAsync(ct);
            return newEvent.ToEventResponse();

        }

        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, EventDto, CancellationToke 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="eventDto">EventDto</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров
        public async Task UpdateEventAsync(Guid id, EventDto eventDto, CancellationToken ct = default) 
        {
            
            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id,ct);
            if (existingEvent is null) 
            {
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            }
            
            _context.Entry(existingEvent).CurrentValues.SetValues(eventDto);//mapping
            await _context.SaveChangesAsync(ct);

        }

        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, Event 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="Event">Event</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах
        public async Task UpdateEventByIdAsync(Guid id, Event Event, CancellationToken ct = default)
        {

            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

            if (existingEvent is null)
            {
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            }
            _context.Entry(existingEvent).CurrentValues.SetValues(Event);//mapping
            await _context.SaveChangesAsync(ct);


        }

        /// <summary>
        /// метод удаления события из хранилища
        /// </summary>
        /// <param name="id">Id события</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task DeleteEventAsync(Guid id, CancellationToken ct) 
        {
            var numberOfRows = await _context.Events.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
            if (numberOfRows == 0) 
            {
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            }
        }

    }
}
