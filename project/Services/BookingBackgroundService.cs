using project.Models;

namespace project.Services
{
    public class BookingBackgroundService (
        IBookingRepository _bookingRepository, 
        ILogger<BookingBackgroundService> _logger): BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    //получаем все брони со всеми статусами   
                    var allBookings = _bookingRepository.GetAllBookings(null, null, null);
                    var pendingBookings = allBookings.BookingsArray.Where(b => b.Status == Models.BookingStatus.Pending);
                    foreach (var pendingBookingResp in pendingBookings)
                    {
                        //имитирующая обращение к внешней системе;
                        await Task.Delay(TimeSpan.FromSeconds(2),stoppingToken);
                        var proccessedBookingDto = new BookingDto
                        {
                            Status = BookingStatus.Confirmed,
                            ProcessedAt = DateTime.UtcNow
                        };
                        _bookingRepository.UpdateBooking(pendingBookingResp.BookingId, proccessedBookingDto);
                    }
                }
            }
            finally 
            {
                _logger.LogInformation("Сервис завершает работу, освобождаем ресурсы");
            }
        }
    }
}
