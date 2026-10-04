using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using project.Models;
using project.Services;
using System.ComponentModel.DataAnnotations;

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
        /// <param name="title">опциональный, регистронезависимый, поиск по названию</param>
        /// <param name="from">опциональный. События, которые начинаются не раньше указанной даты</param>
        /// <param name="to">опциональный. События, которые заканчиваются не позже указанной даты</param>
        /// <param name="page">опциональный. Номер страницы, 1 по умолчанию</param>
        /// <param name="pageSize">опциональный. Размер страницы, 10 по умолчанию</param>
        [HttpGet]
        public ActionResult<PaginatedResult> GetAllEvents(
            [FromQuery] string? title,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть не меньше 1.")] int page = 1,
            [FromQuery, Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100.")] int pageSize = 10) 
        {
            _logger.LogInformation("Вызов метода GetAllEvents: получение всех событий");
            //получить список
            return Ok(_eventService.GetAllEvents(title, from, to, page, pageSize));
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
            var res = _eventService.GetEventById(id);//бросит exception, если не найдено событие с таким id
            //if (res == null)
            //{
            //    return NotFound(new { message = $"Событие с Id {id} не найдено" });
            //}
            return Ok(res);
        }








        //POST /events — создать событие, возвращать корректный HTTP-ответ (например, 201);
        /// <summary>
        /// Создать событие
        /// </summary>
        /// <param name="eventDto">DTO для создания события</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<EventResponse> CreateEvent([FromBody] EventDto eventDto) 
        {
            _logger.LogInformation("Вызов метода CreateEvent для создания нового события");
            //var newId = Guid.NewGuid();
            var newEvent = _eventService.CreateEvent(eventDto);
            //if (!isCreated)
            //{
            //    return Conflict(new { message = $"Событие с Id {newId} уже существует" });
            //}
            return CreatedAtAction
                (
                    nameof(GetEventById),
                    new { id = newEvent.Id},
                    newEvent
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
            _eventService.UpdateEvent(id, eventDto);//бросит exception, если не найдено событие с таким id
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
            _eventService.DeleteEvent(id); // бросит exception, если не найдено событие с таким id
            return Ok(new { message = $"Событие С Id {id} успешно удалено" }); 
                
        }


    }
}
