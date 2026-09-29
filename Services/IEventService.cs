using project.Models;
namespace project.Services
{
    public interface IEventService
    {
        //получить список всех событий
        IReadOnlyList<Event> GetAllEvents(string? title, DateTime? from, DateTime? to);
        //получить событие по id;
        Event? GetEventById(Guid id);
        //создать событие
        //bool CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt);
        Event CreateEvent(EventDto eventDto);

        // обновить событие целиком;
        bool UpdateEvent(Guid id, EventDto eventDto);
        // удалить событие;
        bool DeleteEvent(Guid id);

    }
}
