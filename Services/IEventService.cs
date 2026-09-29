using project.Models;
namespace project.Services
{
    public interface IEventService
    {
        //получить список всех событий
        PaginatedResult GetAllEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);
        //получить событие по id;
        Event GetEventById(Guid id);
        //создать событие
        //bool CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt);
        Event CreateEvent(EventDto eventDto);

        // обновить событие целиком;
        void UpdateEvent(Guid id, EventDto eventDto);
        // удалить событие;
        void DeleteEvent(Guid id);

    }
}
