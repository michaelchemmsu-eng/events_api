
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


        //метод для фонового процесса. метод отката
        public async Task ReleaseSeatAsync(Guid eventId) 
        {
            var sempahore = _semaphoreOfEventId.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
            await sempahore.WaitAsync();

            try
            {
                if (!_events.TryGetValue(eventId, out Event? existingEvent))
                {
                    throw new KeyNotFoundException($"Событие с идентификатором {eventId} не найдено.");
                }
                existingEvent.ReleaseSeats();
                //одновить событие в хранилище
                _events[eventId] = existingEvent;

            }
            catch (Exception) 
            {
                throw;
            }
            finally
            {
                sempahore.Release();
            }


        }





        public async Task TryReserveSeats(Guid eventId) 
        {
            var sempahore = _semaphoreOfEventId.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
            await sempahore.WaitAsync();

            try
            {
                if (!_events.TryGetValue(eventId, out Event? existingEvent))
                {
                    throw new KeyNotFoundException($"Событие с идентификатором {eventId} не найдено.");
                }
                if (!existingEvent.TryReserveSeats()) 
                {
                    throw new NoAvailableSeatsException($"Нет доступных мест на событие с Id {eventId}");
                }
                //одновить событие в хранилище
                _events[eventId] = existingEvent;

            }
            finally 
            {
                sempahore.Release();
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
        public async Task UpdateEvent(Guid id, EventDto eventDto, CancellationToken ct = default) 
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
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах
        public void UpdateEventById(Guid id, Event Event)
        {

            var updatedEvent = new Event
            {
                Id = id,
                Title = Event.Title!,
                Description = Event.Description,
                StartAt = Event.StartAt!,
                EndAt = Event.EndAt!,
                TotalSeats = Event.TotalSeats,
                AvailableSeats =Event.AvailableSeats

            };
            while (true)
            {
                if (!_events.TryGetValue(id, out Event? existingEvent))
                {
                    throw new KeyNotFoundException($"Событие с идентификатором {id} не найдено.");
                }
                // Атомарная замена
                // Если за время замены другой поток успел его заменить, 
                // TryUpdate вернет false, и цикл повторится со свежим existingEvent
                if (_events.TryUpdate(id, updatedEvent, existingEvent))
                {
                    return;
                }
            }

        }

        /// <summary>
        /// метод удаления события из хранилища
        /// </summary>
        /// <param name="id">Id события</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void DeleteEvent(Guid id) 
        {
            if (!_events.TryRemove(id, out Event? obj))
            {
                throw new KeyNotFoundException($"Событие с идентификатором {id} не найдено.");
            }
        }

    }
}
