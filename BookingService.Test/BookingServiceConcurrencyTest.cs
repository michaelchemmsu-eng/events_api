using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using project.CustomExceptions;
using project.DataAccess;
using project.Models;
using project.Repositories;
using project.Services;
using Testcontainers.PostgreSql;
using Xunit;


namespace BookingService.Test
{
    public class BookingServiceConcurrencyTest: IAsyncLifetime
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
        /*
        тест проверяет проблему овербукинга
        создить ивент , сохранить его в БД
        запустить несколько потоков бронирования одного билета через сервис букинга
        проверить сколько потоков завершились успешно, сколько потоков с фейлом 
         */
        public async Task CreateBooking_ConcurrentRequests_PreventsOverbooking() 
        {
            //Arrange
            await ResetDatabaseAsync();
            int NQueries = 10;
            int Nseats = 2;
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = Nseats
            };
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //создать 10 потоков 
            var TaskList = new List<Task>();
            for (int i = 0; i < NQueries; i++) 
            {
                TaskList.Add(Task.Run(() =>
                {
                    //каждый Task создает свой экземпляр контекста, репозитория и сервиса, потому что сервис BookingService не потокобезопасный, так как использует EF Core DbContext, который не потокобезопасный
                    var context = CreateContext();
                    var eventRepository = new EventRepository(context);
                    var bookingRepository = new BookingRepository(context);
                    var eventService = new EventService(eventRepository);

                    var IloggerMock = new Mock<ILogger<project.Services.BookingService>>();
                    var bookingService = new project.Services.BookingService(IloggerMock.Object, eventService, bookingRepository);
                    return bookingService.CreateBookingAsync(EventObject.Id);
                }));
            }
            try
            {

                await Task.WhenAll(TaskList);
            }
            catch
            {
            }
            int succededTasksCount = TaskList.Count(t => t.IsCompletedSuccessfully);
            int faultedTaskCount = TaskList.Count(t => t.IsFaulted);

            //подсчет успешных тасков
            
            Assert.Equal(EventObject.TotalSeats, succededTasksCount);

            // Считаем упавшие таски
           
            Assert.Equal(NQueries - Nseats, faultedTaskCount);

            //проверяем что Falted tasks threw NoAvailableSeatsException 
            var failedTasks = TaskList.Where(t => t.IsFaulted);
            foreach (var task in failedTasks)
            {
                Assert.IsType<NoAvailableSeatsException>(task.Exception?.InnerException);
            }

            // Проверяем состояние события в хранилище, то есть свободных мест должно остаться 0
            var context2 = CreateContext();
            var eventRepository2 = new EventRepository(context2);
            var updatedEvent = await eventRepository2.GetEventByIdAsync(EventObject.Id, TestContext.Current.CancellationToken);
            Assert.Equal(0, updatedEvent!.AvailableSeats);

        }






        [Fact]
        /*
        Тест на уникальность Id при конкурентных запросах
         */
        public async Task ConcurrentQueries_BookingsWithUniqueIds()
        {
            //Arrange
            await ResetDatabaseAsync();
            int NQueries = 10;
            int Nseats = 2;
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = Nseats
            };
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //создать 10 потоков 
            var TaskList = new List<Task>();
            for (int i = 0; i < NQueries; i++)
            {
                TaskList.Add(Task.Run(() =>
                {
                    //каждый Task создает свой экземпляр контекста, репозитория и сервиса, потому что сервис BookingService не потокобезопасный, так как использует EF Core DbContext, который не потокобезопасный
                    var context = CreateContext();
                    var eventRepository = new EventRepository(context);
                    var bookingRepository = new BookingRepository(context);
                    var eventService = new EventService(eventRepository);

                    var IloggerMock = new Mock<ILogger<project.Services.BookingService>>();
                    var bookingService = new project.Services.BookingService(IloggerMock.Object, eventService, bookingRepository);
                    return bookingService.CreateBookingAsync(EventObject.Id);
                }));
            }
            try
            {

                await Task.WhenAll(TaskList);
            }
            catch
            {
            }

            //assert
            int succededTasksCount = TaskList.Count(t => t.IsCompletedSuccessfully);
            var contextAssert = CreateContext();
            var bookingRepositoryAssert = new BookingRepository(contextAssert);
            var bookingIds = await bookingRepositoryAssert.GetPendingBookingIds(TestContext.Current.CancellationToken);
            Assert.Equal(succededTasksCount, bookingIds.Count);
            Assert.Equal(bookingIds.Count, bookingIds.Distinct().Count());//проверяем что все Id уникальны


        }



        [Fact]
        //два потока конкурируют за единственное место в Event
        //оба потока сохраняют в кеш контекста одно и то же состояние EVent
        //второй поток успевает зарезервировать и сохранить
        //первый поток падает с ошибкой NoAvailableSeats при сохранении в БД
        public async Task ConcurrentQueries_ReserveSeatsWhenSecondThrowTookAllSeats() 
        {
            //Arrange
            await ResetDatabaseAsync();
            int Nseats = 1;
            using var context = CreateContext();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = Nseats
            };
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //два независимых контекста
            using var context1 = CreateContext();
            using var context2 = CreateContext();

            //по два экзмепляра ивент и букинг сервисов
            var eventRepos1 = new EventRepository(context1);
            var bookingRepos1 = new BookingRepository(context1);
            var eventService1 = new EventService(eventRepos1);
            var IloggerMock = new Mock<ILogger<project.Services.BookingService>>();
            var bookingService1 = new project.Services.BookingService(IloggerMock.Object, eventService1, bookingRepos1);

            var eventRepos2 = new EventRepository(context2);
            var bookingRepos2 = new BookingRepository(context2);
            var eventService2 = new EventService(eventRepos2);
            var bookingService2 = new project.Services.BookingService(IloggerMock.Object, eventService2, bookingRepos2);

            //сохраним в каждый контекст по экземпляру Event. Оба в одинаковом состоянии
            var eventFromDBInContext1 = await eventRepos1.GetEventByIdAsync(EventObject.Id, TestContext.Current.CancellationToken);
            var eventFromDBInContext2 = await eventRepos2.GetEventByIdAsync(EventObject.Id, TestContext.Current.CancellationToken);

            Assert.Equal(1, eventFromDBInContext1!.AvailableSeats);
            Assert.Equal(1, eventFromDBInContext2!.AvailableSeats);

            //первый букинг сервис броинрует и схораняет
            var bookigInfoContext1 =  await bookingService1.CreateBookingAsync(EventObject.Id, TestContext.Current.CancellationToken);
            Assert.Equal(BookingStatus.Pending, bookigInfoContext1.Status);

            //второй букинг сервис падает с ошибкой NoAvailableSeats
            await Assert.ThrowsAsync<NoAvailableSeatsException>( 
                () => bookingService1.CreateBookingAsync(EventObject.Id, TestContext.Current.CancellationToken));




        }

    }
}
