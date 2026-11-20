
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using project.CustomExceptions;
using project.DataAccess;
using project.Models;
using project.Repositories;
using project.Services.Specifications;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
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

            //OperationCancelledException, KeyNotFoundException, NoAvailableSeatsException
            bool res = await _eventRepository.TryReserveSeatAsync(eventId, ct);
            if (res)
                return;

            //выяснсяем по какой причине updatedRows =0
            //до следующей строки другой поток может добавить событие, но маловероятно, что оно при добавлении будет c Id=eventId
            var eventExists = await _eventRepository.IsExistInRepositoryAsync(eventId, ct);
            if (!eventExists)
            {
                throw new KeyNotFoundException($"Событие {eventId} не найдено.");
            }
            else
            {
                throw new NoAvailableSeatsException($"Нет доступных мест для события {eventId}");
            }

          

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

            var eventFromDB = await _eventRepository.GetEventByIdAsync(id, ct);
            if (eventFromDB is null)
                throw new KeyNotFoundException($"Event with the key {id} is not found");
            int TakenSeatsCount = eventFromDB.TotalSeats - eventFromDB.AvailableSeats;
            if (TakenSeatsCount > eventDto.TotalSeats)
                throw new NoAvailableSeatsException("Total seats cannot be less than the number of seats already taken");

            eventFromDB.UpdateFromDto(eventDto);
            eventFromDB.AvailableSeats -= TakenSeatsCount;


            _eventRepository.UpdateEvent(eventFromDB);
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
