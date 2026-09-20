using Microsoft.AspNetCore.Components.Forms;
using project.Models;

namespace project.Services
{
    public class EventService:IEventService
    {
        private List<Event> _events = new();

        public List<Event> GetAllEvents()
        {
            return _events;
        }
        
        public Event? GetEventById(Guid id)
        {
            return _events.FirstOrDefault(e => e.Id == id);
        }

        public void CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt) 
        {
            var obj = _events.FirstOrDefault(e => e.Id == Id);
            if (obj is not null) 
            {
                _events.Add(new Event { Id = Id, Description = Description, StartAt = StartAt, EndAt = EndAt });
            }
        }
        public bool UpdateEvent(Guid id, EventDto eventDto) 
        {
            var obj = _events.FirstOrDefault(e => e.Id == id);
            if (obj is not null)
            {
                obj.Title = eventDto.Title;
                obj.Description = eventDto.Description;
                obj.StartAt = eventDto.StartAt;
                obj.EndAt = eventDto.EndAt;
                return true;
            }
            return false;
        }

        public bool DeleteEvent(Guid id) 
        {
            var index = _events.FindIndex(e => e.Id == id);
            if (index != -1) 
            {
                _events.RemoveAt(index);
                return true;
            }
            return false;
        }

    }
}
