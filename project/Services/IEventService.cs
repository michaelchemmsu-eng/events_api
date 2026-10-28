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
        /// <summary>
        /// Возвращает сущность Event, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище 
        /// </summary>
        /// <param name="id">Id события Event</param>
        /// <returns>Event or throws KeyNotFoundException </returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в другиз сервисах -- возвращает Event
        Event GetEventById(Guid id);

        //создать событие
        //bool CreateEvent(Guid Id, String title, string? Description, DateTime StartAt, DateTime EndAt);
        EventResponse CreateEvent(EventDto eventDto);


        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, EventDto, CancellationToke 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="eventDto">EventDto</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров
        Task UpdateEvent(Guid id, EventDto eventDto,CancellationToken ct = default);

        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, Event 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="Event">Event</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах
        void UpdateEventById(Guid id, Event Event);
        /// <summary>
        /// удалить событие. В качестве параметра -- Id события Event.
        /// Если события с Id нет в хранилище, то бросается исключение KeyNotFoundException 
        /// </summary>
        /// <param name="id"></param>
        void DeleteEvent(Guid id);
        /// <summary>
        /// Проверяет на наличие события Event с id в хранилище.
        /// возвращает объект Task с булевым значением
        /// </summary>
        /// <param name="Id"> Id события </param>
        /// <param name="ct"> токен отмены </param>
        /// <returns>Task&lt;bool&gt;</returns>
        // проверка события по Id на наличие в репозитории -- это обращение к БД. Поэтому асинхронная функция
        Task<bool> IsExistInRepositoryAsync(Guid Id, CancellationToken ct);

        /// <summary>
        /// атомарно резервирует одно место для события.
        /// метод принимает Id события - идентификатоор события, на которое получен запрос бронирования.
        /// </summary>
        /// <param name="evenId">идентификатоор события</param>
        /// <returns></returns>
        Task TryReserveSeats(Guid evenId);
        /// <summary>
        /// атомарно откатывает бронирование -- возвращает место 
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        Task ReleaseSeatAsync(Guid eventId);

    }
}
