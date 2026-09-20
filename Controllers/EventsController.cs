using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using project.Models;
using project.Services;

namespace project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController(IEventService _eventService, ILogger<EventsController> _logger) : ControllerBase 
    {
        //GET /events — получить список всех событий;
        /// <summary>
        /// Получить список всех событий
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<List<Event>> GetAllEvents() 
        {
            _logger.LogInformation("Вызов метода GetAllEvents: получение всех событий");
            //получить список
            return Ok(_eventService.GetAllEvents());
        }






        //GET /events/{id} — получить событие по id; если не найдено — вернуть корректный HTTP-ответ (например, 404);
        /// <summary>
        /// Получить событие по id
        /// </summary>
        /// <param name="id">id события</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public ActionResult<Event> GetEventById(Guid id)
        {
            _logger.LogInformation("Вызов метода GetEventById для события с ID: {EventId}", id);
            var res = _eventService.GetEventById(id);
            return Ok(res);
        }








        //POST /events — создать событие, возвращать корректный HTTP-ответ (например, 201);
        /// <summary>
        /// Создать событие
        /// </summary>
        /// <param name="eventDto">DTO для создания события</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<Event> CreateEvent([FromBody] EventDto eventDto) 
        {
            _logger.LogInformation("Вызов метода CreateEvent для создания нового события");
            var newId = Guid.NewGuid();
            _eventService.CreateEvent(newId, eventDto.Title, eventDto.Description, eventDto.StartAt, eventDto.EndAt);
            return CreatedAtAction
                (
                    nameof(GetEventById),
                    new { id = newId },
                    _eventService.GetEventById(newId)
                );
        }








        //PUT /events/{id} — обновить событие целиком; если не найдено — вернуть корректный HTTP-ответ (например, 404);
        /// <summary>
        /// Обновить событие целиком
        /// </summary>
        /// <param name="id">id события</param>
        /// <param name="eventDto">DTO для обновления события</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public IActionResult UpdateEvent(Guid id, [FromBody] EventDto eventDto) 
        {
            _logger.LogInformation("Обновление события с ID: {EventId}", id);
            _eventService.UpdateEvent(id, eventDto);
            return Ok(new { message = $"Событие С Id {id} успешно обновлено" });
               
        }








        //DELETE /events/{id} — удалить событие; если не найдено — вернуть корректный HTTP-ответ (например, 404).
        /// <summary>
        /// Удалить событие
        /// </summary>
        /// <param name="id">id события</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public IActionResult DeleteEvent(Guid id) 
        {
            _logger.LogInformation("Удаление события с ID: {EventId}", id);
            _eventService.DeleteEvent(id);
            return Ok(new { message = $"Событие С Id {id} успешно удалено" }); 
                
        }


    }
}
