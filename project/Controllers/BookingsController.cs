using Microsoft.AspNetCore.Mvc;
using project.Services;

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
    }
}
