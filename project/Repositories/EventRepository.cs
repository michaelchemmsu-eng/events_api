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




        public async Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct)
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

    
        public async Task UpdateEventAsync(Event Event, CancellationToken ct = default)
        {
            _context.Events.Update(Event);
            //await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteEventAsync(Event Event, CancellationToken ct = default)
        {
            _context.Events.Remove(Event);
            //await _context.SaveChangesAsync(ct);
            
        }




        public async Task<int> CountAsync(Specification<Event> specification, CancellationToken ct = default) 
        {
            var query = _context.Events.AsQueryable();
            query = SpecificationEvaluator.GetQuery(query, specification);
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


        

    }
}
