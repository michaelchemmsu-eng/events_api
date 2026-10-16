using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace project.Services
{
    public class EventService:IEventService
    {
        private ConcurrentDictionary<Guid,Event> _events = new();

        public async Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct) 
        {
            //иммитация обращения к БД
            await Task.Delay(500,ct);  
            return _events.ContainsKey(id);
        }


        public PaginatedResult GetAllEvents(
            string? title,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 10
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

            var query = _events.Values.AsEnumerable();

           
            
            //фильтруем по title
            if (!string.IsNullOrEmpty(title))
            {
                // регистронезависимый поиск подстроки в заголовке
                query = query.Where(e => e.Title.Contains(title,StringComparison.OrdinalIgnoreCase));
            }
            if (from.HasValue) 
            {
                query = query.Where(e => e.StartAt >= from.Value);
            }
            if (to.HasValue) 
            {
                query = query.Where(e => e.EndAt <= to.Value);
            }
            var filteredEvents = query.ToList();
            int totalCount = filteredEvents.Count;


            var pagedEvents = filteredEvents
                        .OrderBy(e => e.StartAt)//для тестирования сортируем по StartAt, так как ConcurrentDictionary не гарантирует порядок элементов. В тесте ожидаемые элементы хранятся в List.
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(ev => ev.ToEventResponse())
                        .ToArray();

            return new PaginatedResult
            {
                totalEvents = totalCount,
                eventArray = pagedEvents,
                currentPage = page,
                pageSizeOfCurrentPage = pagedEvents.Length
            };
            
        }


        /// <summary>
        /// Возвращает сущность EventResponse, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище 
        /// </summary>
        /// <param name="id">Id события Event</param>
        /// <returns>EventResponse or throws KeyNotFoundException </returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров -- возвращает Dto
        public EventResponse GetEventResponseById(Guid id)
        {
            //return _events.TryGetValue(id, out Event? retVal) ? retVal: null;
            if (!_events.TryGetValue(id, out var retVal))
            {
                throw new KeyNotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }
            return retVal.ToEventResponse();
        }


        /// <summary>
        /// Возвращает сущность Event, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Event or throws KeyNotFoundException</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах -- возвращает Event
        public Event GetEventById(Guid id)
        {
            //return _events.TryGetValue(id, out Event? retVal) ? retVal: null;
            if (!_events.TryGetValue(id, out var retVal))
            {
                throw new KeyNotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }
            return retVal;
        }


        /// <summary>
        /// метод создания сущности Event.
        /// </summary>
        /// <param name="eventDto"></param>
        /// <returns></returns>
        public EventResponse CreateEvent(EventDto eventDto) 
        {
            try 
            {
                var newEvent = Event.Create(eventDto);//бросает исключение, если eventDto.TotalSeats!.Value < 0
                _events[newEvent.Id] = newEvent;
                return newEvent.ToEventResponse();
            }
            catch (ValidationException ex) 
            {
                throw;
            }
            
        }

        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, EventDto 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="eventDto">EventDto</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров
        public void UpdateEvent(Guid id, EventDto eventDto) 
        {
            
            var updatedEvent = new Event 
            {
                Id = id,
                Title = eventDto.Title!,//Title is annotated with required
                Description = eventDto.Description,
                StartAt = eventDto.StartAt!.Value,//автоматическая валидация модели EventDto не позволит StartAt быть null
                EndAt = eventDto.EndAt!.Value//автоматическая валидация модели EventDto не позволит StartAt быть null

            };
            while (true) 
            {
                if (!_events.TryGetValue(id,out Event? existingEvent))
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


        public void DeleteEvent(Guid id) 
        {
            if (!_events.TryRemove(id, out Event? obj))
            {
                throw new KeyNotFoundException($"Событие с идентификатором {id} не найдено.");
            }
        }

    }
}
