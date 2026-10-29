using project.Models;
namespace project.Services
{
    public interface IEventService
    {
        /// <summary>
        /// Асинхронный метод.
        /// Возвращает все строки в таблице Events. Результат будет отфильтрован
        /// по первым трем переданным в аргументах значениям,
        /// а результат пагинарован.
        /// </summary>
        /// <param name="title">параметр фильтрации по заголовку</param>
        /// <param name="from">параметр фильтрации по дате начала события</param>
        /// <param name="to">параметр фильтрации по дате окончания события</param>
        /// <param name="page">номер страницы результата</param>
        /// <param name="pageSize">размер страницы</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        Task<PaginatedResult> GetAllEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, CancellationToken ct = default);

        /// <summary>
        /// асинхронный метод.
        /// Возвращает сущность EventResponse, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище 
        /// </summary>
        /// <param name="id">Id события Event</param>
        /// <param name="ct">токен отмены</param>
        /// <returns>EventResponse or throws KeyNotFoundException </returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров -- возвращает Dto
        Task<EventResponse> GetEventResponseById(Guid id, CancellationToken ct = default);
        /// <summary>
        /// Асинхронный метод.
        /// Возвращает сущность Event, если в хранилище есть объект Event с переданным Id
        /// Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище
        /// </summary>
        /// <param name="id">идентификатор сущности</param>
        /// <param name="ct">токен отмены</param>
        /// <returns>Event or throws KeyNotFoundException</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах -- возвращает Event
        Task<Event>GetEventByIdAsync(Guid id, CancellationToken ct = default);


        /// <summary>
        /// метод создания сущности Event.
        /// </summary>
        /// <param name="eventDto">Dto для создания новой сущности</param>
        /// <param name="ct">токен отмены</param>
        /// <returns></returns>
        Task<EventResponse> CreateEventAsync(EventDto eventDto, CancellationToken ct = default);


        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, EventDto, CancellationToke 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="eventDto">EventDto</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для контроллеров
        Task UpdateEventAsync(Guid id, EventDto eventDto,CancellationToken ct = default);

        /// <summary>
        /// метод обновления события.
        /// принимает в качестве параметра Id события для обновления, Event 
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="Event">Event</param>
        /// <param name="ct">токен отмены</param>
        /// <exception cref="KeyNotFoundException"></exception>
        // метод для бизнес-логики в других сервисах
        Task UpdateEventByIdAsync(Guid id, Event Event,CancellationToken ct= default);
        /// <summary>
        /// удалить событие. В качестве параметра -- Id события Event.
        /// Если события с Id нет в хранилище, то бросается исключение KeyNotFoundException 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        Task DeleteEventAsync(Guid id, CancellationToken ct = default);
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
        Task TryReserveSeatsAsync(Guid evenId, CancellationToken ct);
        /// <summary>
        /// атомарно откатывает бронирование -- возвращает место 
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        Task ReleaseSeatAsync(Guid eventId);

    }
}
