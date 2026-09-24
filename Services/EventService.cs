using Microsoft.AspNetCore.Components.Forms;
using project.Models;
using project.Excpetions;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;

namespace project.Services
{
    public class EventService:IEventService
    {
        private ConcurrentDictionary<Guid,Event> _events = new();

        public IReadOnlyList<Event> GetAllEvents()
        {
            return _events.Values.ToList();
        }
        
        public Event? GetEventById(Guid id)
        {
            return _events.TryGetValue(id, out Event retVal) ? retVal: null;
        }

        public bool CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt) 
        {
            if (_events.TryGetValue(Id, out _))
            {
                return false;
            }
            _events[Id] = new Event { Id = Id, Title = title, Description = Description, StartAt = StartAt, EndAt = EndAt };
            return true;
        }
        public bool UpdateEvent(Guid id, EventDto eventDto) 
        {
            var obj = GetEventById(id);
            if (obj == null)
            {
               return false;
            }
            obj.Title = eventDto.Title;
            obj.Description = eventDto.Description;
            obj.StartAt = eventDto.StartAt;
            obj.EndAt = eventDto.EndAt;
            return true;

        }

        public bool DeleteEvent(Guid id) 
        {
            if (!_events.TryRemove(id, out Event obj))
            {
                return false;
            }
            return true;
        }

    }
}
