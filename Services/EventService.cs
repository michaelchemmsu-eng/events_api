using Microsoft.AspNetCore.Components.Forms;
using project.Models;
using project.Excpetions;

namespace project.Services
{
    public class EventService:IEventService
    {
        private List<Event> _events = new();

        public List<Event> GetAllEvents()
        {
            return _events;
        }
        
        public Event GetEventById(Guid id)
        {
            return _events.FirstOrDefault(e => e.Id == id)?? throw new EventNotFoundExcpetion(id);
        }

        public void CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt) 
        {
            var obj = _events.FirstOrDefault(e => e.Id == Id);
            if (obj is not null) 
            {
                _events.Add(new Event { Id = Id, Description = Description, StartAt = StartAt, EndAt = EndAt });
            }
        }
        public void UpdateEvent(Guid id, EventDto eventDto) 
        {
            var obj = _events.FirstOrDefault(e => e.Id == id);
            
            obj.Title = eventDto.Title;
            obj.Description = eventDto.Description;
            obj.StartAt = eventDto.StartAt;
            obj.EndAt = eventDto.EndAt;
            
           
        }

        public void DeleteEvent(Guid id) 
        {
            var obj = GetEventById(id);
            _events.Remove(obj);
        }

    }
}
