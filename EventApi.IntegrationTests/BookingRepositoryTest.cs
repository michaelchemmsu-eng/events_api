using Microsoft.EntityFrameworkCore;
using Npgsql;
using project.DataAccess;
using project.Models;
using project.Repositories;
using project.Services.Specifications;
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





        
        [Theory]

        [InlineData(null, null, null, null, null)]
        [InlineData("SPIDER NIOR", null, null, null, null)]
        [InlineData("SPIDER NIOR", -4.0, null, null, null)]
        [InlineData("SPIDER NIOR", null, -2.0, null, null)]
        [InlineData("SPIDER NIOR", -2.0, -2.0, null, null)]
        [InlineData(null, null, null, 2, 2)]
        [InlineData(null, null, null, 2, 100)]
        public async Task GetAllBookingsFilteredAsync_FilteredResults(
            string? title,
            double? fromOffsetHours,
            double? toOffsetHours,
            int? page,
            int? pageSize)
        {
            //arrange
            await ResetDatabaseAsync();
            using var contextArrange = CreateContext();
            var bookingRepository = new BookingRepository(contextArrange);
            var eventRepository = new EventRepository(contextArrange);

            //создать ивенты
            /*
             создать букинги для этих ивентов
            сохранить их все в памяти
            сохрнаить их все в БД
            в памяти сымитировать работу фильтра и получить результаты
            применить фильтр в  GetAllBookingsFilteredAsync и получить результаты
            сравнить резултаты
             */
            var referenceDate = DateTime.UtcNow;
            var eventDto1 = new EventDto
            {
                Title = "SPIDER NIOR",
                Description = "2D",
                StartAt = referenceDate.AddHours(-5),
                EndAt = referenceDate.AddHours(-3),
                TotalSeats = 80
            };
            var eventDto2 = new EventDto
            {
                Title = "SPIDER NIOR",
                Description = "3D",
                StartAt = referenceDate.AddHours(-4),
                EndAt = referenceDate.AddHours(-2),
                TotalSeats = 80
            };
            var eventDto3 = new EventDto
            {
                Title = "SPIDER NIOR",
                Description = "3D IMAX",
                StartAt = referenceDate.AddHours(-3),
                EndAt = referenceDate.AddHours(-1),
                TotalSeats = 80
            };
            var event1 = Event.Create(eventDto1);
            var event2 = Event.Create(eventDto2);
            var event3 = Event.Create(eventDto3);


            var eventDto4 = new EventDto
            {
                Title = "SHREK 1",
                Description = "2D",
                StartAt = referenceDate.AddHours(-5),
                EndAt = referenceDate.AddHours(-3),
                TotalSeats = 80
            };
            var eventDto5 = new EventDto
            {
                Title = "SHREK 1",
                Description = "3D",
                StartAt = referenceDate.AddHours(-4),
                EndAt = referenceDate.AddHours(-2),
                TotalSeats = 80
            };
            var eventDto6 = new EventDto
            {
                Title = "SHREK 1",
                Description = "3D IMAX",
                StartAt = referenceDate.AddHours(-3),
                EndAt = referenceDate.AddHours(-1),
                TotalSeats = 80
            };
            var event4 = Event.Create(eventDto4);
            var event5 = Event.Create(eventDto5);
            var event6 = Event.Create(eventDto6);

            await eventRepository.AddEventAsync(event1);
            await eventRepository.AddEventAsync(event2);
            await eventRepository.AddEventAsync(event3);
            await eventRepository.AddEventAsync(event4);
            await eventRepository.AddEventAsync(event5);
            await eventRepository.AddEventAsync(event6);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            var ListOFEvents = new List<Event> { event1, event2, event3, event4, event5, event6};



            var booking1 = Booking.Create(new BookingRequest { EventId = event1.Id });
            var booking2 = Booking.Create(new BookingRequest { EventId = event1.Id });
            var booking3 = Booking.Create(new BookingRequest { EventId = event1.Id });
            var booking4 = Booking.Create(new BookingRequest { EventId = event2.Id });
            var booking5 = Booking.Create(new BookingRequest { EventId = event2.Id });
            var booking6 = Booking.Create(new BookingRequest { EventId = event2.Id });
            var booking7 = Booking.Create(new BookingRequest { EventId = event3.Id });
            var booking8 = Booking.Create(new BookingRequest { EventId = event3.Id });
            var booking9 = Booking.Create(new BookingRequest { EventId = event3.Id });
            var booking10 = Booking.Create(new BookingRequest { EventId = event4.Id });
            var booking11 = Booking.Create(new BookingRequest { EventId = event4.Id });
            var booking12 = Booking.Create(new BookingRequest { EventId = event4.Id });
            var booking13 = Booking.Create(new BookingRequest { EventId = event5.Id });
            var booking14 = Booking.Create(new BookingRequest { EventId = event5.Id });
            var booking15 = Booking.Create(new BookingRequest { EventId = event5.Id });
            var booking16 = Booking.Create(new BookingRequest { EventId = event6.Id });
            var booking17 = Booking.Create(new BookingRequest { EventId = event6.Id });
            var booking18 = Booking.Create(new BookingRequest { EventId = event6.Id });

            var ListOfBookings = new List<Booking> {
                booking1, booking2,booking3,
                booking4, booking5,booking6,
                booking7, booking8,booking9,
                booking10, booking11,booking12,
                booking13, booking14,booking15,
                booking16, booking17,booking18
            };

            var ListOfBookingAddingTasks = new List<Task>();

            foreach (Booking b in ListOfBookings) 
            {
                ListOfBookingAddingTasks.Add(bookingRepository.AddBookingAsync(b));
            }
            await Task.WhenAll(ListOfBookingAddingTasks);
            await bookingRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            /*
              string? title,
            int? fromOffsetHours,
            int? toOffsetHours,
            int? page,
            int? pageSize
             */



            var InMemoryFilteringQuery = ListOfBookings.AsQueryable();
            if (title is not null)
                InMemoryFilteringQuery = InMemoryFilteringQuery.Where(b => b.Event.Title == title);
            if (fromOffsetHours is not null)
                InMemoryFilteringQuery = InMemoryFilteringQuery.Where(b => b.Event.StartAt >= referenceDate.AddHours(fromOffsetHours.Value));
            if (toOffsetHours is not null)
                InMemoryFilteringQuery = InMemoryFilteringQuery.Where(b => b.Event.EndAt <= referenceDate.AddHours(toOffsetHours.Value));
            InMemoryFilteringQuery = InMemoryFilteringQuery.OrderBy(b => b.Event.StartAt).ThenBy(b => b.Id);
            if (page is not null && pageSize is not null) 
            {
                InMemoryFilteringQuery = InMemoryFilteringQuery.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }
            var expectedResult = InMemoryFilteringQuery.ToList();





            using var contexAct = CreateContext();
            var bookingRepositoryAct = new BookingRepository(contexAct);
            DateTime? fromFilter = fromOffsetHours.HasValue ? referenceDate.AddHours(fromOffsetHours.Value) : null;
            DateTime? toFilter = toOffsetHours.HasValue ? referenceDate.AddHours(toOffsetHours.Value) : null;
            var spec = new BookingSearchSpecification(title, fromFilter, toFilter, page, pageSize);
            var actualResult = await bookingRepositoryAct.GetAllBookingsFilteredAsync(spec, TestContext.Current.CancellationToken);


            //assert
            Assert.Equal(expectedResult.OrderBy(b=>b.Id).Select(b => b.Id), actualResult.OrderBy(b => b.Id).Select(b => b.Id));
            


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
