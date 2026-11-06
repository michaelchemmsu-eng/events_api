using project.Models;
using project.Services.EventSpecificator;
namespace project.Repositories
{
    /// <summary>
    /// интерфейс репозитория объектов событий
    /// </summary>
    public interface IEventRepository
    {

        /// <summary>
        /// позволяет получить список объектов событий в соответсвии с спецификацией запроса 
        /// </summary>
        /// <param name="specification">объект спецификации</param>
        /// <param name="ct">токен отмены</param>
        /// <returns>List$ltEvent$gt</returns>
        Task<List<Event>> ListEventsFilteredPagedAsync(Specification<Event> specification, CancellationToken ct = default);

        /// <summary>
        /// Возвращает число объектов событий в соответсвии с спецификацией запроса
        /// </summary>
        /// <param name="specification"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<int> CountAsync(Specification<Event> specification, CancellationToken ct = default);


        /// <summary>
        /// Возвращает список всех объектов событий
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<Event>> GetEventsAsync(CancellationToken ct = default);

        /// <summary>
        /// Возвращает объект события из репозитория по его ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Event?> GetEventByIdAsync(Guid id, CancellationToken ct = default);

        
        /// <summary>
        /// обновить объект события в репозитории
        /// </summary>
        /// <param name="Event"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateEventAsync(Event Event, CancellationToken ct = default);
        
        /// <summary>
        /// добавить объект события в репозиторий
        /// </summary>
        /// <param name="Event"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddEventAsync(Event Event, CancellationToken ct = default);
        /// <summary>
        /// удалить объект события из репозитория
        /// </summary>
        /// <param name="Event"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteEventAsync(Event Event, CancellationToken ct = default);
        /// <summary>
        /// Проверяет, существует ли объект события в репозитории по его ID
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> IsExistInRepositoryAsync(Guid Id, CancellationToken ct);

        /// <summary>
        /// Сохраняет все внесенные изменения в одной транзакции
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken ct);


    }
}
