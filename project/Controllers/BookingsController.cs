using Microsoft.AspNetCore.Mvc;
using project.Services;
using System.ComponentModel.DataAnnotations;

namespace project.Controllers
{
    [ApiController]
    [Route("bookings")]
    public class BookingsController(IBookingService _bookingService, ILogger<BookingsController> _logger):ControllerBase
    {
        /// <summary>
        /// возвращает текущее состояние брони по её идентификатору;
        /// </summary>
        /// <param name="bookingId">идентификатор бронирования</param>
        /// <returns></returns>
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetBookingStatusAsync(Guid bookingId) 
        {
            try
            {
                _logger.LogInformation("попытка получить бронирование с ID {bookingId}",bookingId);
                var bookingResp = await _bookingService.GetBookingByIdAsync(bookingId);

                return Ok(bookingResp);

            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Бронирование с идентификатором {bookingId} не найдено", bookingId);
                //problem details
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Бронирование не найдено",
                    Detail = $"Бронирование с Id {bookingId} не найдено",
                    Instance = HttpContext.Request.Path
                };
                return NotFound(problemDetails);//код 404
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Ошибка при получении бронирования с {bookingId}", bookingId);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }



        /*
        получить все объекты бронирований
        можно передать параметры фильтрации:
        title - заголовок события,
        from - дата начала события
        to - дата окончания события 

         */
        [HttpGet]
        
        public async Task<IActionResult> GetAllBookingsAsync(
            [FromQuery] string? title,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть не меньше 1.")] int page = 1,
            [FromQuery, Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100.")] int pageSize = 10,
            CancellationToken ct = default

            ) 
        {
            //у нас есть букинг сервис
            //нужно создать спецификацию в сервисе 
            //передать эту спецификацию в метод репозитория
            return Ok(await _bookingService.GetAllBookingsAsync(title, from, to, page, pageSize));

        }
    }
}
