using project.Models;
namespace project.Services
{
    public interface IEventService
    {
        //получить список всех событий
        PaginatedResult GetAllEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);
        
        /// <summary>
        /// Возвращает сущность EventResponse, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище 
        /// </summary>
        /// <param name="id">Id события Event</param>
        /// <returns>EventResponse or throws KeyNotFoundException </returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров -- возвращает Dto
        EventResponse GetEventResponseById(Guid id);
        Event GetEventById(Guid id);

        //создать событие
        //bool CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt);
        EventResponse CreateEvent(EventDto eventDto);


        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, EventDto 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="eventDto">EventDto</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров
        void UpdateEvent(Guid id, EventDto eventDto);

        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, Event 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="Event">Event</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах
        void UpdateEventById(Guid id, Event eventDto);
        // удалить событие;
        void DeleteEvent(Guid id);

    }
}
