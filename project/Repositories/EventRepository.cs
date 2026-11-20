using Microsoft.EntityFrameworkCore;
using project.CustomExceptions;
using project.DataAccess;
using project.Models;
using project.Services.EventSpecificator;
namespace project.Repositories

{
    //паттерн Repository - слой доступа к данным, который инкапсулирует логику взаимодействия с источником данных (например, базой данных) и предоставляет абстрактный интерфейс для работы с данными.
    public class EventRepository:IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task SaveChangesAsync(CancellationToken ct)

        {
            bool isSaved = false;
            int retryCount = 0;

            while (!isSaved && retryCount < 5)
            {
                try
                {
                    await _context.SaveChangesAsync(ct);
                    isSaved = true;

                }
                catch (DbUpdateConcurrencyException ex) 
                {
                    //получить объект entry из-за которого исклбчение 
                    //если это не команда модификации, то пробросить искл
                    //если команда модификации, то проверить AvailableSeats - оно Concurrency Token
                    //получить значение свойства из БД
                    //проверить, сколько мест занято в БД
                    //проверить хватит ли число мест , которое идет на модификацию.
                    retryCount++;
                    var entry = ex.Entries.FirstOrDefault(e => e.Entity is Event);
                    
                    if (entry is null || entry.State != EntityState.Modified)
                        throw;
                    var entity = (Event)entry.Entity;






                    //актуальные значения из БД
                    var databaseValues = await entry.GetDatabaseValuesAsync(ct);
                    if (databaseValues is null)
                    {
                        throw new KeyNotFoundException($"Событие {entity.Id} было удалено другим пользователем.");
                    }
                    int currentDbAvailableSeats = (int)databaseValues[nameof(Event.AvailableSeats)]!;
                    int currentDbTotalSeats = (int)databaseValues[nameof(Event.TotalSeats)]!;







                    int takenSeatsCount = currentDbTotalSeats - currentDbAvailableSeats;
                    if (takenSeatsCount > entity.TotalSeats)
                    {
                        throw new NoAvailableSeatsException($"Нет доступных мест для события {entity.Id}");
                    }
                    entity.AvailableSeats = entity.TotalSeats - takenSeatsCount;
                    entry.OriginalValues.SetValues(databaseValues);


                }
            }
            if (!isSaved)
            {
                throw new Exception("Не удалось сохранить изменения из-за высокой нагрузки.");
            }
        }


        public async Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct = default)
        {
            bool IsExist = await _context.Events.AnyAsync(e => e.Id == id, ct);
            return IsExist;
        }

        public async Task<List<Event>> GetEventsAsync(CancellationToken ct = default)
        {
           return await _context.Events.ToListAsync(ct);
        }

        public async Task<Event?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            var EventObj = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
            return EventObj;
        }

        public async Task AddEventAsync(Event Event,CancellationToken ct = default)
        {
            await _context.Events.AddAsync(Event, ct);
        }

    
        public void UpdateEvent(Event Event)
        {
            _context.Events.Update(Event);
            //await _context.SaveChangesAsync(ct);
        }

        public void DeleteEvent(Event Event)
        {
            _context.Events.Remove(Event);
            //await _context.SaveChangesAsync(ct);
            
        }




        public async Task<int> CountAsync(Specification<Event> specification, CancellationToken ct = default) 
        {
            var query = _context.Events.AsQueryable();
            query = SpecificationEvaluator.GetQuery(query, specification,true);
            return await query.CountAsync(ct);
        }





        //переделать или поменять название
        /// <summary>
        /// принимает спецификацию. По спецификации фильтрует строки БД.
        /// </summary>
        /// <param name="specification"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<Event>> ListEventsFilteredPagedAsync(Specification<Event> specification, CancellationToken ct = default) 
        {
            var query = _context.Events.AsQueryable();
            query = SpecificationEvaluator.GetQuery(query, specification);
            return await query.ToListAsync(ct);
        }


        public async Task<bool> TryReserveSeatAsync(Guid eventId, CancellationToken ct = default) 
        {
           //Запрос к БД
            var updatedRows = await _context.Events
                .Where(e => e.Id == eventId && e.AvailableSeats > 0)
                .ExecuteUpdateAsync(setter => setter.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - 1), ct);
            return updatedRows > 0;
           
        }



    }
}
