using Microsoft.Extensions.Logging;
using Moq;
using project.CustomExceptions;
using project.Models;
using project.Services;
using System.ComponentModel.DataAnnotations;
using project.Repositories;
namespace BookingService.Test
{
  
    public class BookingServiceTests
    {

        //Успешные сценарии:
        //создание брони для существующего события — возвращается BookingInfo со статусом Pending;
        
        [Fact]
        public async Task CreateBookingForExistingEvent_ReturnsBookingInfoWithPendingStatus()
        {

            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();


            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(new Mock<ILogger<project.Services.BookingService>>().Object, eventService, bookingRepositoryMock.Object);


            //Arrange
            //создаем событие через EventService
            //обращается к методам _eventRepository AddEventAsync, SaveChangesAsync -- настроить их

            var eventDto = new EventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(3),
                TotalSeats = 15

            };
            var eventExisting = Event.Create(eventDto);


            //получаем Id созданного события
            var eventId = eventExisting.Id;

            eventRepositoryMock.Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(eventExisting);
            //Act
            //настроить bookingRepository.AddBookingAsync: Task
            var result = await bookingService.CreateBookingAsync(eventId);//вызывает eventService.ReserveSeatsAsync, который вызывает eventRepository.GetEventByIdAsync -- объект мока и возвращает объект, значит надо настроить
            //Assert
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
        }





        
        //создание нескольких броней для одного события — все создаются с уникальными Id;
        [Fact]
        public void CreateBookingsWithTheSameEventId_UniqueBookingIds() 
        {
            //Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            //настройки моков



            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(new Mock<ILogger<project.Services.BookingService>>().Object, eventService, bookingRepositoryMock.Object);


            var eventDto = new EventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(3),
                TotalSeats = 15

            };
            var eventExisting = Event.Create(eventDto);


            //получаем Id созданного события
            var eventId = eventExisting.Id;

            List<Guid> uniqueIds = new List<Guid>();

            //настройка
            eventRepositoryMock.Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(eventExisting);


            for (int i = 0; i < 5; i++) 
            {
                //_bookingRepository.AddBookingAsync возвращает Task -> не настраиваем
                //_eventRepository.GetEventByIdAsync(eventId, ct) -> Event -> настриваем
                var res = bookingService.CreateBookingAsync(eventId);
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
        public async Task GetBookingById_ReturnsCorrectBookingInfo()
        {

            //Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(new Mock<ILogger<project.Services.BookingService>>().Object, eventService, bookingRepositoryMock.Object);

            //var createdBooking = bookingService.CreateBookingAsync(eventId).Result;
            var createdBooking = Booking.Create(new BookingRequest { EventId = Guid.NewGuid() });

            bookingRepositoryMock.Setup(b => b.GetBookingByIdAsync(createdBooking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(createdBooking);

            //Act
            //bookingService.GetBookingByIdAsync -> _bookingRepository.GetBookingByIdAsync: booking?
            var retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);
            //Assert
            Assert.Equal(createdBooking.Id, retrievedBooking.BookingId);
            Assert.Equal(createdBooking.EventId, retrievedBooking.EventId);
            Assert.Equal(createdBooking.Status, retrievedBooking.Status);
        }

       

        //получение брони отражает изменение статуса (после Confirm/Reject).
        [Fact]
        public async Task GetBookingById_ReflectsStatusChange()
        {

            // 1. Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);

            // Создаем бронь (изначально у неё статус Pending!)
            var booking = Booking.Create(new BookingRequest { EventId = Guid.NewGuid() });

            // Настраиваем мок: когда сервис попросит эту бронь из репозитория — отдаем наш объект booking
            bookingRepositoryMock
                .Setup(b => b.GetBookingByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            // 2. Act — ВЫЗЫВАЕМ РЕАЛЬНЫЙ МЕТОД подтверждения (а не ставим статус руками!)
            // Например, если метод на самой сущности:
            booking.Confirm();
            // (ИЛИ если метод в сервисе: await bookingService.ConfirmBookingAsync(booking.Id);)

            // Теперь запрашиваем бронь через сервис (используем await, а не .Result!)
            var retrievedBooking = await bookingService.GetBookingByIdAsync(booking.Id);

            // 3. Assert — проверяем, что статус реально сменился с Pending на Confirmed и проставлена дата
            Assert.NotNull(retrievedBooking);
            Assert.Equal(BookingStatus.Confirmed, retrievedBooking.Status);

        }

       



       

       //Неуспешные сценарии:
       //создание брони для несуществующего события;
       [Fact]
       public async Task CreateBooking_ForNonExistentEvent_ThrowsException()
       {
            //Arrange
            var eventId = Guid.NewGuid();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
               new Mock<ILogger<project.Services.BookingService>>().Object,
               eventService,
               bookingRepositoryMock.Object);
            eventRepositoryMock.Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(()=>null);

           //Act & Assert
           KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
               async () =>
                       {
                           await bookingService.CreateBookingAsync(eventId);//eventService.ReserveSeatsAsync->eventRepository.GetEventByIdAsync
                       });
       }


      







       //создание брони для удалённого события;
       [Fact]
       public async Task CreateBookingForDeletedEvent_ThrowsExcetion() 
       {
            // 1. Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);

            var existingEvent = Event.Create(new EventDto
            {
                Title = "Test Event",
                Description = "Test Description",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(3),
                TotalSeats = 15
            });

            // Настраиваем последовательность ответов мока для одного и того же Id:
            eventRepositoryMock
                .SetupSequence(r => r.GetEventByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvent) // 1-й раз (при вызове DeleteEventAsync) — событие ещё есть в БД
                .ReturnsAsync((Event?)null); // 2-й раз (при вызове CreateBookingAsync) — события уже нет (удалено)!

            // Удаляем событие через сервис (тут сработает 1-й ответ из SetupSequence)
            await eventService.DeleteEventAsync(existingEvent.Id, It.IsAny<CancellationToken>());

            // 2. Act & Assert
            // Пытаемся создать бронь для удаленного события (тут сработает 2-й ответ — null)
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => bookingService.CreateBookingAsync(existingEvent.Id));

            // Заодно можем проверить, что метод удаления в репозитории реально вызывался:
            eventRepositoryMock.Verify(
                r => r.DeleteEvent(existingEvent),
                Times.Once);

        }

       










        //получение брони по несуществующему Id.
        [Fact]
        public async Task GetBookingByNonExistingBookingId_ThrowsException() 
        {
            //Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);

            //bookingRepository.GetBookingByIdAsync

            Guid NonExistingBookingId = Guid.NewGuid();
            bookingRepositoryMock
                .Setup(b => b.GetBookingByIdAsync(NonExistingBookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking?)null);

            //Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => bookingService.GetBookingByIdAsync(NonExistingBookingId));


            bookingRepositoryMock.Verify(b=>b.GetBookingByIdAsync(NonExistingBookingId),Times.Once);
        }


       





        //Создание брони уменьшает AvailableSeats на 1.
        [Fact]
        public async Task CreateBooking_LowersAvailableSeatsByOne()
        {
            //Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);



            var eventCreated = Event.Create
               (
                   new EventDto
                   {
                       Title = "Test Event 1",
                       Description = "Test Description 1",
                       StartAt = DateTime.UtcNow.AddDays(1),
                       EndAt = DateTime.UtcNow.AddDays(3),
                       TotalSeats = 2
                   }
               );

            //eventRepository.GetEventByIdAsync
            eventRepositoryMock.Setup(r => r.GetEventByIdAsync(eventCreated.Id, It.IsAny<CancellationToken>())).ReturnsAsync(eventCreated);

            //Act
            var bookingResponseObject = await bookingService.CreateBookingAsync(eventCreated.Id);
            //
  
            //Assert
            Assert.Equal(1, eventCreated.AvailableSeats);
            eventRepositoryMock.Verify(r => r.UpdateEvent(eventCreated), Times.Once);
            eventRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);


        }

     







       //После исчерпания мест следующая попытка выбрасывает NoAvailableSeatsException.
       [Fact]
       public async Task CreateBookingOfTheEventWithNoAvailableSeats_ThrowsNoAvailableSeatsException()
       {
            //Arrange

            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);


            var eventResponse = Event.Create
              (
                  new EventDto
                  {
                      Title = "Test Event 1",
                      Description = "Test Description 1",
                      StartAt = DateTime.UtcNow.AddDays(1),
                      EndAt = DateTime.UtcNow.AddDays(3),
                      TotalSeats = 1
                  }
              );
           var eventResponseId = eventResponse.Id;
            //Act
            //bookingService.CreateBookingAsync -> eventService.ReserveSeatsAsync->eventRepository.GetEventByIdAsync
            //eventRepository.UpdateEvent
            //eventRepository.SaveChangesAsync
            eventRepositoryMock.Setup(r => r.GetEventByIdAsync(eventResponseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(eventResponse);
            var bookingResponseObject = await bookingService.CreateBookingAsync(eventResponseId);
           //Act & Assert
           NoAvailableSeatsException exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
           {
               await bookingService.CreateBookingAsync(eventResponseId);
           });

            eventRepositoryMock.Verify(r => r.UpdateEvent(eventResponse), Times.Once);
            eventRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);


        }


       






    //Создание нескольких броней (до лимита) — все успешны, у каждой уникальный Id.
    [Fact]
    public async Task CreatedBookingsUntilAvailbaleSeatsPresent_NoExceptionsAndUniqueIds() 
    {
            //Arrange

            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);

            int Nseats = 3;

            var eventResponse = Event.Create
              (
                  new EventDto
                  {
                      Title = "Test Event 1",
                      Description = "Test Description 1",
                      StartAt = DateTime.UtcNow.AddDays(1),
                      EndAt = DateTime.UtcNow.AddDays(3),
                      TotalSeats = Nseats
                  }
              );
            var eventResponseId = eventResponse.Id;
       
            var tasks = new List<Task<BookingResponse>>();

            //Act
            /*
             bookingService.CreateBookingAsync -> 
                eventService.ReserveSeatsAsync ->
                    eventRepository.GetEventByIdAsync
                    eventRepository.UpdateEvent
                    eventRepository.SaveChangesAsync
             */
            eventRepositoryMock.Setup(r => r.GetEventByIdAsync(eventResponseId, It.IsAny<CancellationToken>())).ReturnsAsync(eventResponse);
            for (int i = 0; i < Nseats; i++) 
            {
                var task = bookingService.CreateBookingAsync(eventResponse.Id);
                tasks.Add(task);
            }
            BookingResponse[] bookingResponses = await Task.WhenAll(tasks);
            //assert
            //проверить, что все успешны
            Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
            Assert.Equal(Nseats, bookingResponses.Length);
            //проверить уникальность Id
            var uniqueIdsCount = bookingResponses.Select(b => b.BookingId).Distinct().Count();
            Assert.Equal(Nseats, uniqueIdsCount);

            eventRepositoryMock.Verify(r => r.UpdateEvent(eventResponse), Times.Exactly(Nseats));
            eventRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(Nseats));

        }


       

        //Бронирование при отсутствии мест → NoAvailableSeatsException.
        [Fact]
        public async Task BookingEventWithNoAvailableSeats_ThrowNoAvailableSeatsException() 
        {
            //Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);

            int Nseats = 1;

            var eventResponse = Event.Create
              (
                  new EventDto
                  {
                      Title = "Test Event 1",
                      Description = "Test Description 1",
                      StartAt = DateTime.UtcNow.AddDays(1),
                      EndAt = DateTime.UtcNow.AddDays(3),
                      TotalSeats = Nseats
                  }
              );
            eventResponse.AvailableSeats = 0;
            var eventResponseId = eventResponse.Id;

            //Act & Assert
            /*
            bookingService.CreateBookingAsync -> 
               eventService.ReserveSeatsAsync ->
                   eventRepository.GetEventByIdAsync
                   eventRepository.UpdateEvent
                   eventRepository.SaveChangesAsync
            */
            eventRepositoryMock.Setup(r => r.GetEventByIdAsync(eventResponseId, It.IsAny<CancellationToken>())).ReturnsAsync(eventResponse);
            NoAvailableSeatsException exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await bookingService.CreateBookingAsync(eventResponseId, TestContext.Current.CancellationToken);
            });

            eventRepositoryMock.Verify(r => r.UpdateEvent(eventResponse), Times.Never);
            eventRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        }


       

        //тесты на смену статуса брони:
        //После вызова Confirm() бронь возвращает статус Confirmed и заполненный ProcessedAt.
        [Fact]
        public async Task ConfirmBooking_ChangedBookingStatus() 
        {
            // Arrange создаем саму сущность Booking напрямую
            var booking = Booking.Create(new BookingRequest { EventId = Guid.NewGuid() });

            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            bookingRepositoryMock
                .Setup(b => b.GetBookingByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);

            // Act 
            booking.Confirm();

            // И запрашиваем бронь через сервис
            /*
             bookingService.GetBookingByIdAsync ->
                bookingRepository.GetBookingByIdAsync: Booking?
             */
            var response = await bookingService.GetBookingByIdAsync(booking.Id);

            // Assert проверяем, что статус стал Confirmed и дата ProcessedAt заполнилась
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }


        





        //тесты на смену статуса брони:
        //После вызова Reject() бронь возвращает статус Rejected и заполненный ProcessedAt.
        [Fact]
        public async Task RejectBooking_ChangedBookingStatus()
        {
            // Arrange создаем саму сущность Booking напрямую
            var booking = Booking.Create(new BookingRequest { EventId = Guid.NewGuid() });

            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();

            bookingRepositoryMock
                .Setup(b => b.GetBookingByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);

            // Act 
            booking.Reject();

            // И запрашиваем бронь через сервис
            /*
             bookingService.GetBookingByIdAsync ->
                bookingRepository.GetBookingByIdAsync: Booking?
             */
            var response = await bookingService.GetBookingByIdAsync(booking.Id);

            // Assert проверяем, что статус стал Confirmed и дата ProcessedAt заполнилась
            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);

        }





        

    //После Reject() -> ReleaseSeats() -> количество свободных мест восстанавливается.
    [Fact]
    public async Task RejectAndReleaseSeats_RestoreSeats() 
    {
            // Arrange создаем саму сущность Booking напрямую
            var booking = Booking.Create(new BookingRequest { EventId = Guid.NewGuid() });
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            bookingRepositoryMock
                .Setup(b => b.GetBookingByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);
            var eventService = new EventService(eventRepositoryMock.Object);
            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);
            // Act 
            booking.Reject();
            // ReleaseSeats() на событии
            var eventObj = Event.Create(new EventDto 
            { 
                Title = "Test Event", 
                Description = "Test Description", 
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(3), 
                TotalSeats = 10 }
            );
            int expectedAvailableSeats = eventObj.AvailableSeats+1;
            eventObj.ReleaseSeats();
            int actualAvailbaleSeats = eventObj.AvailableSeats;
            var response = await bookingService.GetBookingByIdAsync(booking.Id);

            // Assert проверяем, что статус стал Rejected и дата ProcessedAt заполнилась
            Assert.Equal(BookingStatus.Rejected, response.Status);
            Assert.NotNull(booking.ProcessedAt);
            Assert.Equal(expectedAvailableSeats, actualAvailbaleSeats);
    }









        




        //После Reject() -> ReleaseSeats() -> можно успешно создать новую бронь на то же место.
        [Fact]
        public async Task RejectAndReleaseSeats_BookingAvailbleForRestoredSeats()
        {
            //Arrange
           
            
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();

            var eventService = new EventService(eventRepositoryMock.Object);

            var bookingService = new project.Services.BookingService(
                new Mock<ILogger<project.Services.BookingService>>().Object,
                eventService,
                bookingRepositoryMock.Object);


            //создаем событие, на которое нет свободных мест
            var eventDto = new EventDto
            {
                Title = "Title",
                Description = "Desctription",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(3),
                TotalSeats = 1
            };
            var eventObject = Event.Create(eventDto);
            eventObject.AvailableSeats = 0;
            var booking = Booking.Create(new BookingRequest { EventId = eventObject.Id});

            //Act
            booking.Reject();
            eventObject.ReleaseSeats();//совобождаем одно место на это событие. Было 0, стало 1
            bookingRepositoryMock
                            .Setup(b => b.GetBookingByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(booking);
            eventRepositoryMock
                            .Setup(b => b.GetEventByIdAsync(eventObject.Id, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(eventObject);

            var bookingResponse = await bookingService.GetBookingByIdAsync(booking.Id);
            var eventResponse = await eventService.GetEventResponseById(eventObject.Id);

            Assert.Equal(1, eventResponse.AvailableSeats);
            Assert.Equal(BookingStatus.Rejected, bookingResponse.Status);

            //создадаим бронь
            bookingResponse = await bookingService.CreateBookingAsync(eventObject.Id);
            Assert.Equal(BookingStatus.Pending, bookingResponse.Status);
        }



        



        /*




//Тест на уникальность Id при конкурентных запросах
[Theory]
[InlineData(1)]   // Тест запустится для 1 места
[InlineData(5)]   // Тест запустится для 5 мест
[InlineData(50)]  // Тест запустится для 50 мест
public async Task ConcurrentQueries_BookingsWithUniqueIds(int totalSeats) 
{
 //Arrange
 int ExtraQueries = 10;
 int NQueries = totalSeats + ExtraQueries;
 var eventResponse = _eventService.CreateEvent
  (
      new EventDto
      {
          Title = "Test Event",
          Description = "Test Description",
          StartAt = DateTime.UtcNow.AddDays(1),
          EndAt = DateTime.UtcNow.AddDays(3),
          TotalSeats = totalSeats
      }
  );
 var TaskList = new List<Task>();
 for (int i = 0; i < NQueries; i++)
 {

     TaskList.Add(_bookingService.CreateBookingAsync(eventResponse.Id));

 }
 //Дано: событие на totalSeats мест, NQueries конкурентных запросов.
 //Ожидается: ровно totalSeats успешных броней, X — NoAvailableSeatsException, AvailableSeats = Y.
 // Act
 try
 {

     await Task.WhenAll(TaskList);
 }
 catch
 {
 }
 // Assert
 //проверяем, что все booking с уникальными Id
 //получить все booking
 //получить id всех booking
 //проверить на единственность 
 var BookingIds = _bookingRepository.GetAllBookings().Select(b => b.BookingId);
 foreach (var id in BookingIds) 
 {
     Assert.Single(BookingIds, id);
 }         

}









[Fact]
public void CreateEvent_WhenTotalSeatsIsZeroOrNegative_ThrowsValidationException()
{
 // Arrange
 var dtoWithZeroSeats = new EventDto
 {
     Title = "Title",
     StartAt = DateTime.UtcNow.AddDays(1),
     EndAt = DateTime.UtcNow.AddDays(2),
     TotalSeats = 0 
 };
 var dtoWithNegativeValueSeats = new EventDto
 {
     Title = "Title",
     StartAt = DateTime.UtcNow.AddDays(1),
     EndAt = DateTime.UtcNow.AddDays(2),
     TotalSeats = -1
 };

 // Act and Assert: проверяем, что выбрасывается ValidationException 
 Assert.Throws<ValidationException>(() =>_eventService.CreateEvent(dtoWithZeroSeats));
 Assert.Throws<ValidationException>(() => _eventService.CreateEvent(dtoWithNegativeValueSeats));

}
*/

    }


}
