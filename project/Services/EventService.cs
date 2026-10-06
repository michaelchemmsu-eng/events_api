using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;

namespace project.Services
{
    public class EventService:IEventService
    {
        private ConcurrentDictionary<Guid,Event> _events = new();

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
        
        public EventResponse GetEventById(Guid id)
        {
            //return _events.TryGetValue(id, out Event? retVal) ? retVal: null;
            if (!_events.TryGetValue(id, out var retVal))
            {
                throw new KeyNotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }
            return retVal.ToEventResponse();
        }

        //public bool CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt) 
        //{
        //    if (_events.TryGetValue(Id, out _))
        //    {
        //        return false;
        //    }
        //    _events[Id] = new Event { Id = Id, Title = title, Description = Description, StartAt = StartAt, EndAt = EndAt };
        //    return true;
        //}

        public EventResponse CreateEvent(EventDto eventDto) 
        {
            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt!.Value,//автоматическая валидация модели EventDto не позволит StartAt быть null
                EndAt = eventDto.EndAt!.Value//автоматическая валидация модели EventDto не позволит StartAt быть null
            };
            _events[newEvent.Id] = newEvent;
            return newEvent.ToEventResponse();
        }


        public void UpdateEvent(Guid id, EventDto eventDto) 
        {
            
            var updatedEvent = new Event 
            {
                Id = id,
                Title = eventDto.Title,
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

        public void DeleteEvent(Guid id) 
        {
            if (!_events.TryRemove(id, out Event? obj))
            {
                throw new KeyNotFoundException($"Событие с идентификатором {id} не найдено.");
            }
        }

    }
}
