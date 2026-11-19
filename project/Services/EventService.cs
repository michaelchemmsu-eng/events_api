
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

  
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
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

           
            await _eventRepository.TryReserveSeatAsync(eventId, ct);
            //OperationCancelledException, KeyNotFoundException, NoAvailableSeatsException
            

            //var eventItem = await _eventRepository.GetEventByIdAsync(eventId, ct);

            //if (eventItem is null)
            //{
            //    throw new KeyNotFoundException($"Событие {eventId} не найдено.");

            //}
            //eventItem.ReserveSeat();//throws NoAvailableSeatsException if AvailableSeats <=0
            //int retryCount = 0;// счетчик попыток обновления объекта в БД при возникновении конфликта конкурентного доступа
            //bool isSaved = false;
            
            //while (!isSaved && retryCount < 5) 
            //{
            //    try
            //    {
            //        _eventRepository.UpdateEvent(eventItem);

            //        //update "Events" set "AvailableSeats" = x where "Id" = '...';
            //        //если другой поток удалит событие, то при попытке обновить psql выдаст 0, а EF Core -- DbUpdateConcurrencyException
            //        //но в контексте в памяти есть еще созданный объект booking, то есть 
            //        //отпарвляется так же команда insert into "Bookings". Эта команда может выбросить ошибку 
            //        //DbUpdateException при вызове команды DbContext.SaveChangesAsync();
            //        await _eventRepository.SaveChangesAsync(ct);
            //        isSaved = true;
            //    }
            //    catch (DbUpdateConcurrencyException ex) 
            //    {
            //        retryCount++;
            //        /*
            //        нужно дойти до свойства AvailableSeats сущности, которая вызвала конфликт
            //        посмотреть на текущее значение этого свойства в БД
            //        если оно 0, то бросить исключение
            //        если оно >0, то обновить Original Value в соответсвии с DataBase value
            //        AvailbaleSeats обновить в соответсвии с DataBase value
            //        повторить попытку обновления на Current Value.
            //            */
            //        //получаем объект entityentry , который вызвал конфликт
            //        var entry = ex.Entries[0]; //Gets the entries that were involved in the error. Typically this is a single entry, but in some cases it may be zero or multiple entries.
            //        //достаем из БД данные. получаем PropertyValues
            //        var databaseValues = await entry.GetDatabaseValuesAsync(ct); //Queries the database for copies of the values of the tracked entity as they currently exist in the database

            //        if (databaseValues is null)
            //        {
            //            throw new KeyNotFoundException($"Событие {eventId} было удалено другим пользователем.");
            //        }

            //        //поулчаем значение конкретного проперти AvailableSeats, которое сейчас в БД
            //        int currentDbSeats = (int)databaseValues[nameof(Event.AvailableSeats)]!;

            //        if (currentDbSeats <= 0)
            //        {
            //            throw new NoAvailableSeatsException($"Нет доступных мест для события {eventId}");
            //        }

            //        eventItem.AvailableSeats = currentDbSeats - 1;
            //        entry.OriginalValues.SetValues(databaseValues);//Original values are the values that were originally
            //    }
            //    catch (DbUpdateException)
            //    {
            //        // Если во время сохранения брони событие было удалено другим потоком 
            //        throw new KeyNotFoundException($"Событие {eventId} было удалено другим пользователем.");
            //    }
            //}
            //if (!isSaved)
            //{
            //    throw new Exception("Не удалось забронировать место из-за высокой нагрузки.");
            //}

            



           

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
            var newEvent = Event.Create(eventDto);//бросает исключение, если eventDto.TotalSeats!.Value <= 0
            await _eventRepository.AddEventAsync(newEvent, ct);
            await _eventRepository.SaveChangesAsync(ct);
            return newEvent.ToEventResponse();

        }

        /// <summary>
        /// метод обновления события в репозитории. 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="eventDto">EventDto</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task UpdateEventAsync(Guid id, EventDto eventDto, CancellationToken ct = default) 
        {

            var IsExists = await _eventRepository.IsExistInRepositoryAsync(id, ct);
            if (!IsExists) 
            {
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            }
            var UpdatedEvent = Event.Create(eventDto);//бросает исключение, если данные не валидные
            UpdatedEvent.Id = id;
            _eventRepository.UpdateEvent(UpdatedEvent);
            await _eventRepository.SaveChangesAsync(ct);

        }


        /// <summary>
        /// метод удаления события из хранилища
        /// </summary>
        /// <param name="id">Id события</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task DeleteEventAsync(Guid id, CancellationToken ct) 
        {
            //делать или не делать .Include(e => e.Bookings) -- вот в чем вопрос
            var existingEvent = await _eventRepository.GetEventByIdAsync(id, ct);
            if (existingEvent is null)
            {
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            }
            try
            {
                _eventRepository.DeleteEvent(existingEvent);
                //елси на событие ссылается объект букинга, то выбросит ошибку DbUpdateException. См на конфигурацию.
                await _eventRepository.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) 
            {
                throw new InvalidOperationException( $"Невозможно удалить событие с ID {id}, так как на него уже существуют бронирования.", ex);
            }


        }

    }
}
