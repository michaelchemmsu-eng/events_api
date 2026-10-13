using project.Services;
using Moq;
using Microsoft.Extensions.Logging;
using project.Models;
using Microsoft.Extensions.Hosting;
namespace BookingService.Test
{
    public class BookingServiceTest
    {
        //public BookingService(IBookingRepository BookingStorage, ILogger<BookingService> Logger
        
        private project.Services.BookingService _bookingService;
        private InMemoryBookingDictionary _bookingRepository;
        private EventService _eventService;


        public BookingServiceTest()
        {

            _bookingRepository = new InMemoryBookingDictionary(); 
            var mockLogger = new Mock<ILogger<project.Services.BookingService>>();//не нужно
            _eventService = new EventService();
            _bookingService = new project.Services.BookingService(_bookingRepository, mockLogger.Object, _eventService);
            
        }


        //Успешные сценарии:

        //создание брони для существующего события — возвращается BookingInfo со статусом Pending;
        
        [Fact]
        public void CreateBookingForExistingEvent_ReturnsBookingInfoWithPendingStatus()
        {
            //Arrange
            //создаем событие через EventService
            _eventService.CreateEvent
                (
                    new EventDto 
                    {
                        Title = "Test Event",
                        Description = "Test Description",
                        StartAt = DateTime.UtcNow.AddDays(1),
                        EndAt = DateTime.UtcNow.AddDays(3)
                    }
                );
            //получаем Id созданного события
            var eventId = _eventService.GetAllEvents(null, null, null).eventArray.First().Id;
            //Act
            //создаем бронь сущесвтующего события через BookingService
            var result = _bookingService.CreateBookingAsync(eventId).Result;
            //Assert
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
        }






        //создание нескольких броней для одного события — все создаются с уникальными Id;
        [Fact]
        public void CreateBookingsWithTheSameEventId_UniqueBookingIds() 
        {
            //Arrange
            //создаем событие через EventService
            _eventService.CreateEvent
                (
                    new EventDto
                    {
                        Title = "Test Event",
                        Description = "Test Description",
                        StartAt = DateTime.UtcNow.AddDays(1),
                        EndAt = DateTime.UtcNow.AddDays(3)
                    }
                );
            //получаем Id созданного события
            var eventId = _eventService.GetAllEvents(null, null, null).eventArray.First().Id;
            List<Guid> uniqueIds = new List<Guid>();
            for (int i = 0; i < 5; i++) 
            {
                var res = _bookingService.CreateBookingAsync(eventId);
                uniqueIds.Add(res.Result.BookingId);
            }
            //для всех элементов коллекции рассматриваемый элемент должен встречаться только один раз
            Assert.All(
                uniqueIds,
                Id => Assert.Single(
                        uniqueIds, 
                        x => x == Id));

        }








        //получение брони по Id — возвращается корректная информация;
        [Fact]
        public void GetBookingById_ReturnsCorrectBookingInfo()
        {
            //Arrange
            //создаем событие через EventService
            _eventService.CreateEvent
                (
                    new EventDto
                    {
                        Title = "Test Event",
                        Description = "Test Description",
                        StartAt = DateTime.UtcNow.AddDays(1),
                        EndAt = DateTime.UtcNow.AddDays(3)
                    }
                );
            //получаем Id созданного события
            var eventId = _eventService.GetAllEvents(null, null, null).eventArray.First().Id;
            var createdBooking = _bookingService.CreateBookingAsync(eventId).Result;
            //Act
            var retrievedBooking = _bookingService.GetBookingByIdAsync(createdBooking.BookingId).Result;
            //Assert
            Assert.Equal(createdBooking.BookingId, retrievedBooking.BookingId);
            Assert.Equal(createdBooking.EventId, retrievedBooking.EventId);
            Assert.Equal(createdBooking.Status, retrievedBooking.Status);
        }

        //получение брони отражает изменение статуса (после Confirm/Reject).
        [Fact]
        public void GetBookingById_ReflectsStatusChange()
        {
            //Arrange
            //создаем событие через EventService
            _eventService.CreateEvent
                (
                    new EventDto
                    {
                        Title = "Test Event",
                        Description = "Test Description",
                        StartAt = DateTime.UtcNow.AddDays(1),
                        EndAt = DateTime.UtcNow.AddDays(3)
                    }
                );
            //получаем Id созданного события
            var eventId = _eventService.GetAllEvents(null, null, null).eventArray.First().Id;
            var createdBooking = _bookingService.CreateBookingAsync(eventId).Result;
            _bookingRepository.UpdateBooking(createdBooking.BookingId, new BookingDto { Status = BookingStatus.Confirmed, ProcessedAt = DateTime.UtcNow });
            //Act
            var updatedBooking = _bookingService.GetBookingByIdAsync(createdBooking.BookingId).Result;
            //Assert
            Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);

        }

        //Неуспешные сценарии:
        //создание брони для несуществующего события;
        [Fact]
        public async Task CreateBooking_ForNonExistentEvent_ThrowsException()
        {
            //Arrange
            var eventId = Guid.NewGuid();
            //Act & Assert
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventId);
            });
        }


        //создание брони для удалённого события;
        [Fact]
        public async Task CreateBookingForDeletedEvent_ThrowsExcetion() 
        {
            //Arrange
            //создаем событие через EventService
            _eventService.CreateEvent
                (
                    new EventDto
                    {
                        Title = "Test Event",
                        Description = "Test Description",
                        StartAt = DateTime.UtcNow.AddDays(1),
                        EndAt = DateTime.UtcNow.AddDays(3)
                    }
                );
            //получаем Id созданного события
            var eventId = _eventService.GetAllEvents(null, null, null).eventArray.First().Id;
            //удаляем событие
            _eventService.DeleteEvent(eventId);
            //Act & Assert
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventId);
            });

        }



        //получение брони по несуществующему Id.
        [Fact]
        public async Task GetBookingByNonExistingBookingId_ThrowsException() 
        {
            //Arrange
            //создаем событие через EventService
            _eventService.CreateEvent
                (
                    new EventDto
                    {
                        Title = "Test Event",
                        Description = "Test Description",
                        StartAt = DateTime.UtcNow.AddDays(1),
                        EndAt = DateTime.UtcNow.AddDays(3)
                    }
                );
            Guid NonExistingBookingId = Guid.NewGuid();
            //Act & Assert
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _bookingService.GetBookingByIdAsync(NonExistingBookingId); ;
            });
        }

    }
}
