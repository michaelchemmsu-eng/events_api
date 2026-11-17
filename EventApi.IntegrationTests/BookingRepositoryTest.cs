using Microsoft.EntityFrameworkCore;
using Npgsql;
using project.DataAccess;
using project.Models;
using project.Repositories;

using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests
{
    [Collection("PostgresContainer collection")]
    public class BookingRepositoryTest
    {

        private record FKInfo(string conrelid, string confrelid, string contype, string confdeltype);

        private readonly PostgresContainerFixture _postgresContainerFixture;
        private Task ResetDatabaseAsync() => _postgresContainerFixture.ResetDatabaseAsync();
        private AppDbContext CreateContext() => _postgresContainerFixture.CreateContext();

        public BookingRepositoryTest(PostgresContainerFixture containerFixture)
        {
            _postgresContainerFixture = containerFixture;
        }







        [Fact]

        public async Task CheckIfTableExistAfterMigration_AllTableNamesMustPresent()
        {
            //после миграции создаются таблицы по схеме и одна таблица __EFMigrationsHistory
            //чтобы узнать , что таблицы создались, делаем прямой запрос к information_schema.tables,
            //возвращаем table_name
            //Arrange
            await ResetDatabaseAsync();
            var context = CreateContext();
            string sqlQuery = $"""Select table_name from information_schema.tables where table_schema = 'public'""";
            var listOFTables = await context.Database.SqlQueryRaw<string>(sqlQuery).ToListAsync();
            var listOFExpectedTables = new List<string> { "Events", "Bookings", "__EFMigrationsHistory" };
            Assert.All(listOFExpectedTables, (tableName) => Assert.Contains(tableName, listOFTables));
        }

        [Fact]
        /*
         после Migrate проверяет записи в __EFMigrationsHistory, 
         */
        public async Task Check__EFMigrationsHistoryAfterMigration_MigrationResultInNotEmptyTable()
        {
            //после миграции создаются таблицы по схеме и одна таблица __EFMigrationsHistory
            //чтобы узнать , что таблицы создались, делаем прямой запрос к information_schema.tables,
            //возвращаем table_name
            //Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            string sqlQuery = $"""Select "MigrationId" from "__EFMigrationsHistory" """;
            //act
            var listOFTables = await context.Database.SqlQueryRaw<string>(sqlQuery).ToListAsync();
            //assert
            Assert.NotEmpty(listOFTables);

        }




        //внешний ключ между Bookings и Events с правилом удаления RESTRICT
        /*
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = 'имя_таблицы'
            ORDER BY ordinal_position;
        select column_name from information_schema.columns where table_name = 'pg_constraint';
         */
        [Fact]
        public async Task CheckFKConstraint_CheckOnDeleteRestrict()
        {
            //таблица pg_constrain
            //select conrelid::regclass, confrelid::regclass,contype,confdeltype from pg_constraint where contype = 'f'
            // arrange
            await ResetDatabaseAsync();
            var context = CreateContext();
            string sqlQuery = $""" select conrelid::regclass::text, confrelid::regclass::text,contype::text,confdeltype::text from pg_constraint where contype = 'f' """;
            
            //act
            var res = await context.Database.SqlQueryRaw<FKInfo>(sqlQuery).ToListAsync();
            //assert
            Assert.Contains(new FKInfo (conrelid : "\"Bookings\"", confdeltype: "r", confrelid :"\"Events\"", contype : "f" ), res);
        }







        [Fact]
        // проверяет, что метод GetBookingByIdAsync возвращает объект бронирования, если он существует в БД.
        public async Task GetBookingByIdAsync_RetrievesAnExistingBookingObject()
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
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);
            await context.Bookings.AddAsync(bookingObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);



            //Act
            using var contextAct = CreateContext();
            var bookingRepositoryAct = new BookingRepository(contextAct);
            var bookingRetrieved = await bookingRepositoryAct.GetBookingByIdAsync(bookingObject.Id, TestContext.Current.CancellationToken);
            //Assert
            Assert.NotNull(bookingRetrieved);
            Assert.Null(bookingRetrieved.Event); // навигационное свойство не загружено
            //остальные Assert
            Assert.Equal(bookingObject.Id, bookingRetrieved.Id);
            Assert.Equal(bookingObject.Status, bookingRetrieved.Status);
            Assert.Equal(bookingObject.Id, bookingRetrieved.Id);
            Assert.Null(bookingRetrieved.ProcessedAt);// еще не обработано, поэтому ProcessedAt должно быть null
            Assert.Equal(bookingObject.EventId, bookingRetrieved.EventId);
            Assert.Equal(bookingObject.CreatedAt, bookingRetrieved.CreatedAt, TimeSpan.FromMicroseconds(1));

        }



        [Fact]
        // проверяет, что метод GetBookingByIdAsync возвращает null, если объекта бронирования с переданным Id нет в БД
        public async Task GetBookingByIdAsync_BookingObjectAbsentInDB_RetrievesNull()
        {
            //Arrange
            await ResetDatabaseAsync();
        
            
            using var contextAct = CreateContext();
            var bookingRepositoryAct = new BookingRepository(contextAct);
            Guid NonExistentBookingId = Guid.NewGuid();
            //Act
            var bookingRetrieved = await bookingRepositoryAct.GetBookingByIdAsync(NonExistentBookingId, TestContext.Current.CancellationToken);
            //Assert
            Assert.Null(bookingRetrieved);

        }



        [Fact]
        //проверяет, что метод AddBookingAsync добавляет в ДБ объект букинга, которго еще нет в БД
        public async Task AddBookingAsync_ObjectAbsentInDB_AddsBookingObject() 
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
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);
            //Act
      
            var bookingRepository = new BookingRepository(context);
            await bookingRepository.AddBookingAsync(bookingObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            //Assert
            using var contextAssert = CreateContext();
            var bookingRetrieved = await contextAssert.Bookings.Where(b => b.Id == bookingObject.Id).FirstOrDefaultAsync(TestContext.Current.CancellationToken);
            //Assert
            Assert.NotNull(bookingRetrieved);
            Assert.Null(bookingRetrieved.Event); // навигационное свойство не загружено
            //остальные Assert
            Assert.Equal(bookingObject.Id, bookingRetrieved.Id);
            Assert.Equal(bookingObject.Status, bookingRetrieved.Status);
            Assert.Equal(bookingObject.Id, bookingRetrieved.Id);
            Assert.Null(bookingRetrieved.ProcessedAt);// еще не обработано, поэтому ProcessedAt должно быть null
            Assert.Equal(bookingObject.EventId, bookingRetrieved.EventId);
            Assert.Equal(bookingObject.CreatedAt, bookingRetrieved.CreatedAt, TimeSpan.FromMicroseconds(1));

        }




        [Fact]
        //проверяет, что метод AddBookingAsync добавляет в ДБ объект букинга, который уже присутсвтует в БД. Бросает Exception
        public async Task AddBookingAsync_ObjectPresentInDB_ThrowsDbUpdateExcpetion()
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
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);
            await context.Bookings.AddAsync(bookingObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act
            using var contextAct = CreateContext();
            var bookingRepository = new BookingRepository(contextAct);
            await bookingRepository.AddBookingAsync(bookingObject, TestContext.Current.CancellationToken);

            //assert
            await Assert.ThrowsAsync<DbUpdateException>(() => contextAct.SaveChangesAsync(TestContext.Current.CancellationToken));
        }



        [Fact]
        //проверяет, что метод AddBookingAsync добавляет в ДБ объект букинга, для которого нет соответсвтующего объекта Event
        //breaking FK constraint
        public async Task AddBookingAsync_CorrespondingEventObjectAbsentInDB_AddsBookingObject()
        {
            //Arrange
            await ResetDatabaseAsync();
    
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObject = Event.Create(EventDtoObject);
            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);

            //act
            using var contextAct = CreateContext();
            var bookingRepository = new BookingRepository(contextAct);
            await bookingRepository.AddBookingAsync(bookingObject, TestContext.Current.CancellationToken);

            //assert
            await Assert.ThrowsAsync<DbUpdateException>(() => contextAct.SaveChangesAsync(TestContext.Current.CancellationToken));
        }


        [Fact]
        // проверяет, что метод UpdateBooking обновляет существующий объект бронирования в БД.
        public async Task UpdateBooking_UpdatesExistingBookingObjectInDB()
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
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);
            await context.Bookings.AddAsync(bookingObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            using var contextAct = CreateContext();
            var bookingRepositoryAct = new BookingRepository(contextAct);
            var bookingToUpdate = await bookingRepositoryAct.GetBookingByIdAsync(bookingObject.Id, TestContext.Current.CancellationToken);
            if (bookingToUpdate is null)
            {
                throw new Exception("Booking object not found in the database.");
            }
            bookingToUpdate.Status = BookingStatus.Confirmed;
            bookingRepositoryAct.UpdateBooking(bookingToUpdate);
            await contextAct.SaveChangesAsync(TestContext.Current.CancellationToken);
            //Assert
            using var contextAssert = CreateContext();
            var bookingRetrieved = await contextAssert.Bookings.Where(b => b.Id == bookingObject.Id).FirstOrDefaultAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(bookingRetrieved);
            Assert.Equal(BookingStatus.Confirmed, bookingRetrieved.Status);
        }


        [Fact]
        // проверяет, что метод UpdateBooking при обновлении несуществующего объекта бронирования в БД выбрасывает ошибку DbUpdateConcurrencyException
        public async Task UpdateBooking_UpdatesNonExistentBookingObjectInDB() 
        {
            //Arrange
            await ResetDatabaseAsync();

            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObject = Event.Create(EventDtoObject);

            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);

            //act
            using var context = CreateContext();
            var bookingRepository = new BookingRepository(context);
            bookingRepository.UpdateBooking(bookingObject);
            //assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        }

        [Fact]
        // проверяет, что метод DeleteBookingAsync удаляет существующий объект бронирования из БД.
        public async Task DeleteBookingAsync_DeletesExistingBookingObjectInDB() 
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
            var EventObject = Event.Create(EventDtoObject);
            await context.Events.AddAsync(EventObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);
            await context.Bookings.AddAsync(bookingObject, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            using var contextAct = CreateContext();
            var bookingRepositoryAct = new BookingRepository(contextAct);
            var bookingToDelete = await bookingRepositoryAct.GetBookingByIdAsync(bookingObject.Id, TestContext.Current.CancellationToken);
            if (bookingToDelete is null)
            {
                throw new Exception("Booking object not found in the database.");
            }
            await bookingRepositoryAct.DeleteBookingAsync(bookingToDelete, TestContext.Current.CancellationToken);
            await bookingRepositoryAct.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Assert
            using var contextAssert = CreateContext();
            var bookingRetrieved = await contextAssert.Bookings.Where(b => b.Id == bookingObject.Id).FirstOrDefaultAsync(TestContext.Current.CancellationToken);
            Assert.Null(bookingRetrieved);
        }


        [Fact]
        // проверяет, что метод DeleteBookingAsync при удалении несуществующего объекта бронирования в БД выбрасывает ошибку DbUpdateConcurrencyException
        public async Task DeleteBookingAsync_DeletesNonExistentBookingObjectInDB_ThrowsException() 
        {
            //Arrange
            await ResetDatabaseAsync();
            var EventDtoObject = new EventDto
            {
                Title = "Title",
                Description = "Description",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 100
            };
            var EventObject = Event.Create(EventDtoObject);
            var bookingRequest = new BookingRequest
            {
                EventId = EventObject.Id
            };
            var bookingObject = Booking.Create(bookingRequest);
            //act
            using var context = CreateContext();
            var bookingRepository = new BookingRepository(context);
            await bookingRepository.DeleteBookingAsync(bookingObject, TestContext.Current.CancellationToken);
            //assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }


        [Fact]
        // проверяет, что метод GetBookingsAsync возвращает все объекты бронирования, связанные с определенным объектом Event в БД.
        public async Task GetBookingsAsync_RetrievesAllBookingObjectsofTheEventFromDB() 
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

            var ListOfBookingsExpected = new List<Booking>();
            ListOfBookingsExpected.Add(BookingObject1);
            ListOfBookingsExpected.Add(BookingObject2);


            await context.Bookings.AddRangeAsync(BookingObject1, BookingObject2);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            using var contextAct = CreateContext();
            var bookingRepositoryAct = new BookingRepository(contextAct);
            var ListOfBookingsActual = await bookingRepositoryAct.GetBookingsAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            //assert
            Assert.Equal(ListOfBookingsExpected.Count, ListOfBookingsActual.Count);

            ListOfBookingsExpected = ListOfBookingsExpected.OrderBy(b => b.Id).ToList();
            ListOfBookingsActual = ListOfBookingsActual.OrderBy(b => b.Id).ToList();

            for (int i = 0; i < ListOfBookingsActual.Count; i++) 
            {
                Assert.Equal(ListOfBookingsExpected[i].Id, ListOfBookingsActual[i].Id);
                Assert.Equal(ListOfBookingsExpected[i].Status, ListOfBookingsActual[i].Status);
                Assert.Equal(ListOfBookingsExpected[i].CreatedAt, ListOfBookingsActual[i].CreatedAt, TimeSpan.FromMicroseconds(1));
                Assert.Null(ListOfBookingsActual[i].ProcessedAt);
                Assert.Null(ListOfBookingsActual[i].Event);
                Assert.Equal(ListOfBookingsExpected[i].EventId, ListOfBookingsActual[i].EventId);
            }


        }


        [Fact]
        // проверяет, что метод GetPendingBookingIds возвращает все Id объектов бронирования со статусом Pending в БД.
        public async Task GetPendingBookingIds_RetrievesAllPendingBookingObjectIdsFromDB() 
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

            var ListOfPendingBookingIdsExpected = new List<Guid>();
            ListOfPendingBookingIdsExpected.Add(BookingObject1.Id);
            ListOfPendingBookingIdsExpected.Add(BookingObject2.Id);


            await context.Bookings.AddRangeAsync(BookingObject1, BookingObject2);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            using var contextAct = CreateContext();
            var bookingRepositoryAct = new BookingRepository(contextAct);
            var ListOfPendingBookingIdsActual = await bookingRepositoryAct.GetPendingBookingIds(TestContext.Current.CancellationToken);
            //assert
            Assert.Equal(ListOfPendingBookingIdsExpected.Count, ListOfPendingBookingIdsActual.Count);

            ListOfPendingBookingIdsExpected = ListOfPendingBookingIdsExpected.OrderBy(id => id).ToList();
            ListOfPendingBookingIdsActual = ListOfPendingBookingIdsActual.OrderBy(id => id).ToList();

            for (int i = 0; i < ListOfPendingBookingIdsActual.Count; i++)
            {
                Assert.Equal(ListOfPendingBookingIdsExpected[i], ListOfPendingBookingIdsActual[i]);
            }

        }




    }
}
