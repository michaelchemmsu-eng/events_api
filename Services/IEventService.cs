using project.Models;
namespace project.Services
{
    public interface IEventService
    {
        //получить список всех событий
        List<Event> GetAllEvents();
        //получить событие по id;
        Event GetEventById(Guid id);
        //создать событие
        void CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt);
        // обновить событие целиком;
        void UpdateEvent(Guid id, EventDto eventDto);
        // удалить событие;
        void DeleteEvent(Guid id);

    }
}
