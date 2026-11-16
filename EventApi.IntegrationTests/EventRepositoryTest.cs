using Microsoft.EntityFrameworkCore;
using Npgsql;
using project.DataAccess;
using project.Models;
using project.Repositories;
using System.Net.NetworkInformation;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests
{
    public class EventRepositoryTest: IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
           .WithDatabase("eventapi")
           .Build();

        public async ValueTask InitializeAsync() => await _postgres.StartAsync();
        public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

        private AppDbContext CreateContext() 
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
        private async Task ResetDatabaseAsync() 
        {
            NpgsqlConnection.ClearAllPools(); 
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        [Fact]
        //тест проверяет, что метод IsExistInRepositoryAsync возвращает false для несуществующего идентификатора события.
        public async Task IsExistInRepository_NonExsitentEventId_False()
        {
            //Arrange
            await ResetDatabaseAsync();
            Guid EventId = Guid.NewGuid();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            //Act
            var result = await eventRepository.IsExistInRepositoryAsync(EventId, TestContext.Current.CancellationToken);
            //Assert
            Assert.False(result);
            
        }

        [Fact]
        // тест проверяет, что метод IsExistInRepositoryAsync возвращает true для существующего идентификатора события.
        public async Task IsExistInRepository_ExistentEventId_True()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var EventDtoObject = new EventDto 
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObjext = Event.Create(EventDtoObject);
            var eventRepository = new EventRepository(context);
            await eventRepository.AddEventAsync(EventObjext, TestContext.Current.CancellationToken);
            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);
            //Act
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var result = await eventRepositoryAct.IsExistInRepositoryAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            //Assert
            Assert.True(result);

        }



        [Fact]
        // проверяет, что метод DeleteEvent удаляет объект существующего в БД события успешно. успех - это возврат true метода репозитория IsExistInRepositoryAsync
        public async Task DeleteEvent_DeletesExistingEventId()
        {
            //arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObjext = Event.Create(EventDtoObject);


            var eventRepository = new EventRepository(context);
            await eventRepository.AddEventAsync(EventObjext, TestContext.Current.CancellationToken);
            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //связанные с этим Event объекты бронирования
            var bookingRequest1 = new BookingRequest
            {
                EventId = EventObjext.Id
            };
            var bookingRequest2 = new BookingRequest
            {
                EventId = EventObjext.Id
            };
            var BookingObject1 = Booking.Create(bookingRequest1);
            var BookingObject2 = Booking.Create(bookingRequest2);
            await context.Bookings.AddRangeAsync(BookingObject1,BookingObject2);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            eventRepositoryAct.DeleteEvent(EventObjext);
            await eventRepositoryAct.SaveChangesAsync(TestContext.Current.CancellationToken);

            //assert
            using var contextAssert = CreateContext();
            var eventRepositoryAssert = new EventRepository(contextAssert);
            var result = await eventRepositoryAssert.IsExistInRepositoryAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            Assert.False(result);
            //настройки ограничиения FK по умолчанию -- DELETE CASCADE
            var ListOfBookings = await contextAssert.Bookings.ToListAsync(TestContext.Current.CancellationToken);
            Assert.Empty(ListOfBookings);

        }






        [Fact]
        // проверяет, что метод DeleteEvent удаляет объект несуществующего в БД события. При сохранении изменений в БД (SaveChangesAsync) выбрасывается исключение DbUpdateConcurrencyException, так как объект не существует в БД.
        public async Task DeleteEvent_DeletesNonExistentEventId_DbUpdateConcurrencyExceptionWhenSaveChagesAsyncInvoced()
        {
            //arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObjext = Event.Create(EventDtoObject);
            //Act and Assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(context);
            eventRepositoryAct.DeleteEvent(EventObjext);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => eventRepositoryAct.SaveChangesAsync(TestContext.Current.CancellationToken));
        }



        [Fact]
        // проверяет, что метод AddEventAsync добавляет объект несуществующего в БД события успешно. успех - это возврат true метода репозитория IsExistInRepositoryAsync
        public async Task AddEventAsync_AddsNonExistentEvent_True() 
        {
            //Arrange and Act
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObjext = Event.Create(EventDtoObject);
            var eventRepository = new EventRepository(context);
            await eventRepository.AddEventAsync(EventObjext, TestContext.Current.CancellationToken);
            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Assert
            using var contextAssert = CreateContext();
            var eventRepositoryAssert = new EventRepository(context);
            var result = await eventRepositoryAssert.IsExistInRepositoryAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            Assert.True(result);
        }

        [Fact]
        // проверяет, что метод AddEventAsync добавляет объект существующего в БД события. При сохранении изменений в БД (SaveChangesAsync) выбрасывается исключение DbUpdateConcurrencyException, так как объект уже существует в БД.
        public async Task AddEventAsync_AddsExistentEvent_DbUpdateConcurrencyExceptionWhenSaveChagesAsyncInvoced() { 
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObjext = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObjext, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act and Assert
            using var contextAssert = CreateContext();
            var eventRepositoryAssert = new EventRepository(contextAssert);
            await eventRepositoryAssert.AddEventAsync(EventObjext, TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<DbUpdateException>(() => eventRepositoryAssert.SaveChangesAsync(TestContext.Current.CancellationToken));

        }

        [Fact]
        // проверяет, что метод GetEventByIdAsync возвращает объект события, если он существует в БД.
        public async Task GetEventByIdAsync_RetrievesAnExistingEventObject() 
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObjext = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObjext, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            //Act
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var EventRetrieved = await eventRepositoryAct.GetEventByIdAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            //Assert
            Assert.NotNull(EventRetrieved);
            Assert.Equal(EventObjext.Title, EventRetrieved.Title);
            Assert.Equal(EventObjext.Description, EventRetrieved.Description);
            //разная точно у C# и PostgreSQL, поэтому сравниваем с точностью до микросекунд
            Assert.Equal(EventObjext.StartAt, EventRetrieved.StartAt, TimeSpan.FromMicroseconds(1));
            Assert.Equal(EventObjext.EndAt, EventRetrieved.EndAt, TimeSpan.FromMicroseconds(1));
            Assert.Equal(EventObjext.TotalSeats, EventRetrieved.TotalSeats);

        }


        [Fact]
        // проверяет, что метод GetEVentByIdAsync() возвращает null, если передать идентификатор события, которого нет в БД
        public async Task GetEventByIdAsync_NonExistentEventObject_RetrievesNull()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            Guid id = Guid.NewGuid();
            //Act
            var result = await eventRepository.GetEventByIdAsync(id, TestContext.Current.CancellationToken);
            //assert
            Assert.Null(result);

        }



        [Fact]
        // проверяет, что метод GetEventsAsync() возвращает все объекты событий, которые существуют в БД.
        public async Task GetEventsAsync_EventsExistInDB_RetrievesAllEventObjects() 
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var EventDtoObject1 = new EventDto
            {
                Title = "Title1",
                Description = "Description1",
                StartAt = DateTime.UtcNow.AddDays(-5),
                EndAt = DateTime.UtcNow.AddDays(-5).AddHours(1),
                TotalSeats = 100
            };
            var EventDtoObject2 = new EventDto
            {
                Title = "Title2",
                Description = "Description2",
                StartAt = DateTime.UtcNow.AddDays(-4),
                EndAt = DateTime.UtcNow.AddDays(-4).AddHours(1),
                TotalSeats = 100
            };
            var EventDtoObject3 = new EventDto
            {
                Title = "Title3",
                Description = "Description3",
                StartAt = DateTime.UtcNow.AddDays(-3),
                EndAt = DateTime.UtcNow.AddDays(-3).AddHours(1),
                TotalSeats = 100
            };
            var EventObject1 = Event.Create(EventDtoObject1);
            var EventObject2 = Event.Create(EventDtoObject2);
            var EventObject3 = Event.Create(EventDtoObject3);
            List<Event> listOfEvents = new List<Event>(3);
            listOfEvents.Add(EventObject1);
            listOfEvents.Add(EventObject2);
            listOfEvents.Add(EventObject3);
            
            await context.Events.AddRangeAsync(EventObject1, EventObject2, EventObject3);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            //Act
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var listOfRetrievedEvents = await eventRepositoryAct.GetEventsAsync(TestContext.Current.CancellationToken);
            //Assert
            var expectedList = listOfEvents.OrderBy(e => e.Id).ToList();
            var actualList = listOfRetrievedEvents.OrderBy(e => e.Id).ToList();
            Assert.Equal(3, listOfRetrievedEvents.Count);
            for (int i = 0; i < expectedList.Count; i++)
            {
                Assert.Equal(expectedList[i].Id, actualList[i].Id);
                Assert.Equal(expectedList[i].Title, actualList[i].Title);
                Assert.Equal(expectedList[i].Description, actualList[i].Description);
                Assert.Equal(expectedList[i].TotalSeats, actualList[i].TotalSeats);
                Assert.Equal(expectedList[i].StartAt, actualList[i].StartAt, TimeSpan.FromMicroseconds(1));
                Assert.Equal(expectedList[i].EndAt, actualList[i].EndAt, TimeSpan.FromMicroseconds(1));
            }


        }



        [Fact]
        // проверяет, что метод GetEventsAsync() возвращает пустой список, если в БД нет объектов событий.
        public async Task GetEventsAsync_EmptyDB_RetrievesEmptyList()
        {
            //Arrange
            await ResetDatabaseAsync();
            //Act
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var listOfRetrievedEvents = await eventRepositoryAct.GetEventsAsync(TestContext.Current.CancellationToken);
            //Assert
            Assert.Empty(listOfRetrievedEvents);

        }

    }
}
