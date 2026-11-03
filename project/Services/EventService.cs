
using project.CustomExceptions;
using project.DataAccess;

using project.Models;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using project.Services.Specifications;
using project.Repositories;
using System.Runtime.InteropServices;

namespace project.Services
{
    /// <summary>
    /// Класс сервиса событий
    /// </summary>
    public class EventService:IEventService
    {

        private AppDbContext _context;//в роли Unit of Work
        private readonly IEventRepository _eventRepository;

        public EventService(AppDbContext context) 
        {
            _context = context;
            _eventRepository = new EventRepository(context);
        }


        public async Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct) 
        {
            bool IsExist = await _eventRepository.IsExistInRepositoryAsync(id,ct);
            return IsExist;
        }



        /// <summary>
        /// Попытка резервировать место для события
        /// </summary>
        /// <param name="eventId">идетификатор события, для которого резервируется 1 место</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NoAvailableSeatsException"></exception>
        public async Task ReserveSeatsAsync(Guid eventId,CancellationToken ct=default) 
        {

            var eventItem = await _eventRepository.GetEventByIdAsync(eventId, ct);

            if (eventItem is null)
            {
                throw new KeyNotFoundException($"Событие {eventId} не найдено.");

            }
            eventItem.ReserveSeat();//throws NoAvailableSeatsException if AvailableSeats <=0
            int retryCount = 0;// счетчик попыток обновления объекта в БД при возникновении конфликта конкурентного доступа
            bool isSaved = false;
            
            while (!isSaved && retryCount < 5) 
            {
                try
                {
                    await _eventRepository.UpdateEventAsync(eventItem, ct);
                    
                    isSaved = true;
                    _context.SaveChanges();
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

                    eventItem.AvailableSeats = currentDbSeats - 1;
                    entry.OriginalValues.SetValues(databaseValues);//Original values are the values that were originally
                }
            }
            if (!isSaved)
            {
                throw new Exception("Не удалось забронировать место из-за высокой нагрузки.");
            }

            ////первый вариант
            ////var updatedRows = await _context.Events
            ////    .Where(e => e.Id == eventId && e.AvailableSeats > 0)
            ////    .ExecuteUpdateAsync(setter => setter.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - 1)
            ////                            ,ct);
            ////if (updatedRows > 0)
            ////    return;
            //////выяснсяем по какой причине updatedRows =0
            //////до следующей строки другой поток может добавить событие, но маловероятно, что оно при добавлении будет c Id=eventId
            ////bool eventExists = await _context.Events.AnyAsync(e => e.Id == eventId, ct);
            ////if (!eventExists)
            ////{
            ////    throw new KeyNotFoundException($"Событие {eventId} не найдено.");
            ////}
            ////else 
            ////{
            ////    throw new NoAvailableSeatsException($"Нет доступных мест для события {eventId}");
            ////}



           

        }


        /// <summary>
        /// Асинхронный метод.
        /// Возвращает все строки в таблице Events. Результат будет отфильтрован
        /// по первым трем переданным в аргументах значениям, если они не null.
        /// а результат пагинарован, если переданы значения параметров пагинации
        /// </summary>
        /// <param name="title">параметр фильтрации по заголовку</param>
        /// <param name="from">параметр фильтрации по дате начала события</param>
        /// <param name="to">параметр фильтрации по дате окончания события</param>
        /// <param name="page">номер страницы результата</param>
        /// <param name="pageSize">размер страницы</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public async Task<PaginatedResult> GetAllEventsPaged(
            string? title,
            DateTime? from,
            DateTime? to,
            int page ,
            int pageSize,
            CancellationToken ct=default
            )
        {
            //создаем спецификацию для фильтрации
            var specification = new EventSearchSpecification(title, from, to, page, pageSize);
            int totalCount = await _eventRepository.CountAsync(specification, ct);
            var items = await _eventRepository.ListEventsFilteredPagedAsync(specification, ct);//получаем отфильтрованные и пагинированные данные
            return new PaginatedResult
            {
                totalEvents = totalCount,
                eventArray = items.Select(e => e.ToEventResponse()).ToArray(),
                currentPage = page,
                pageSizeOfCurrentPage = items.Count
            };

        }


        /// <summary>
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
            var EventObj = await _eventRepository.GetEventByIdAsync(id,ct);
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
        //public async Task <Event> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        //{
        //    var EventObj = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        //    if (EventObj is null)
        //    {
        //        throw new KeyNotFoundException($"Событие с идентификатором '{id}' не найдено.");
        //    }
        //    return EventObj;
        //}


        /// <summary>
        /// метод создания сущности Event в репозитории.
        /// </summary>
        /// <param name="eventDto">Dto для создания новой сущности</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        public async Task <EventResponse> CreateEventAsync(EventDto eventDto, CancellationToken ct = default) 
        {
            var newEvent = Event.Create(eventDto);//бросает исключение, если eventDto.TotalSeats!.Value < 0
            await _eventRepository.AddEventAsync(newEvent, ct);
            await _context.SaveChangesAsync(ct);
            return newEvent.ToEventResponse();

        }

        /// <summary>
        /// метод обновления события в репозитории. 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="eventDto">EventDto</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров
        public async Task UpdateEventAsync(Guid id, EventDto eventDto, CancellationToken ct = default) 
        {

            var existingEvent = await _eventRepository.GetEventByIdAsync(id, ct);
            if (existingEvent is null) 
            {
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            }
            
            _context.Entry(existingEvent).CurrentValues.SetValues(eventDto);//mapping
            await _eventRepository.UpdateEventAsync(existingEvent,ct);
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
        //public async Task UpdateEventByIdAsync(Guid id, Event Event, CancellationToken ct = default)
        //{

        //    var existingEvent = await _eventRepository.GetEventByIdAsync(id,ct);

        //    if (existingEvent is null)
        //    {
        //        throw new KeyNotFoundException($"Event with the key {id} is not found");
        //    }
        //    _context.Entry(existingEvent).CurrentValues.SetValues(Event);//mapping
        //    await _context.SaveChangesAsync(ct);


        //}

        /// <summary>
        /// метод удаления события из хранилища
        /// </summary>
        /// <param name="id">Id события</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task DeleteEventAsync(Guid id, CancellationToken ct) 
        {
            var existingEvent = await _eventRepository.GetEventByIdAsync(id, ct);
            if (existingEvent is null)
            {
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            }
            await _eventRepository.DeleteEventAsync(existingEvent, ct);
            await _context.SaveChangesAsync(ct);


        }

    }
}
