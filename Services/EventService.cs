using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using project.Excpetions;
using project.Models;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;

namespace project.Services
{
    public class EventService:IEventService
    {
        private ConcurrentDictionary<Guid,Event> _events = new();

        public IReadOnlyList<Event> GetAllEvents(
            string? title,
            DateTime? from,
            DateTime? to)
        {
            var query = _events.Values.AsEnumerable();

            //фильтруем по title
            if (!string.IsNullOrEmpty(title))
            {
                query = query.Where(e => e.Title == title);
            }
            if (from.HasValue) 
            {
                query = query.Where(e => e.StartAt >= from.Value);
            }
            if (to.HasValue) 
            {
                query = query.Where(e => e.EndAt <= to.Value);
            }
            return query.ToList();
            
        }
        
        public Event? GetEventById(Guid id)
        {
            return _events.TryGetValue(id, out Event? retVal) ? retVal: null;
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

        public Event CreateEvent(EventDto eventDto) 
        {
            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt
            };
            _events[newEvent.Id] = newEvent;
            return newEvent;
        }


        public bool UpdateEvent(Guid id, EventDto eventDto) 
        {
            
            var updatedEvent = new Event 
            {
                Id = id,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartAt = eventDto.StartAt,
                EndAt = eventDto.EndAt
            };
            while (true) 
            {
                if (!_events.TryGetValue(id,out Event? existingEvent))
                {
                    return false;
                }
                // Атомарная замена
                // Если за время замены другой поток успел его заменить, 
                // TryUpdate вернет false, и цикл повторится со свежим existingEvent
                if (_events.TryUpdate(id, updatedEvent, existingEvent))
                {
                    return true;
                }
            }
           
        }

        public bool DeleteEvent(Guid id) 
        {
            return _events.TryRemove(id, out Event? obj); 
            
        }

    }
}
