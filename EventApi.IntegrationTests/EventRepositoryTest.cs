using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using project.DataAccess;
using project.Models;
using project.Repositories;

using project.Services.Specifications;

namespace EventApi.IntegrationTests
{
    [Collection("PostgresContainer collection")]
    public class EventRepositoryTest
    {
        private readonly PostgresContainerFixture _postgresContainerFixture;
        private Task ResetDatabaseAsync() => _postgresContainerFixture.ResetDatabaseAsync();
        private AppDbContext CreateContext() => _postgresContainerFixture.CreateContext();

        public EventRepositoryTest(PostgresContainerFixture containerFixture) 
        {
            _postgresContainerFixture = containerFixture;
        }


     
     



















        [Fact]
        //update an NonExistent event obj in DB 
        public async Task UpdateAnNonExistentEvent_ThrowsDbUpdatedException()
        {
            //arrange
            DateTime CurrentTime = DateTime.UtcNow;

            var EventDto1 = new EventDto
            {
                Title = "SHREK 1",
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };
            var event1 = Event.Create(EventDto1);

            //act
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            eventRepositoryAct.UpdateEvent(event1);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => eventRepositoryAct.SaveChangesAsync(TestContext.Current.CancellationToken));
            


        }



















        [Fact]
        //update an existing event obj in DB 
        public async Task UpdateAnExistingEvent_UpdatedEvent() 
        {
            //arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepositoryArrange = new EventRepository(context);
            DateTime CurrentTime = DateTime.UtcNow;


            var EventDto1 = new EventDto
            {
                Title = "SHREK 1",
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };
            var event1 = Event.Create(EventDto1);

            await eventRepositoryArrange.AddEventAsync(event1, TestContext.Current.CancellationToken);
            await eventRepositoryArrange.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var EventToUpdate = await eventRepositoryArrange.GetEventByIdAsync(event1.Id, TestContext.Current.CancellationToken);
            if (EventToUpdate is not null)
            {
                EventToUpdate.Title = "SHREK 2";
                eventRepositoryAct.UpdateEvent(EventToUpdate);
                await eventRepositoryAct.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
            else 
            {
                throw new NullReferenceException();
            }
            //assert
            using var contextAssert = CreateContext();
            var eventRepositoryAssert = new EventRepository(contextAssert);
            var UpdatedEvent = await eventRepositoryAssert.GetEventByIdAsync(event1.Id, TestContext.Current.CancellationToken);
            if (UpdatedEvent is not null)
            {
                Assert.Equal("SHREK 2", UpdatedEvent.Title);

            }
            else 
            {
                throw new NullReferenceException();
            }


        }














        [Fact]
        //проверяем что результат фильтрации отсортирован по OrderBy = e => e.StartAt; ThenBy = e => e.Id;
        public async Task ListEventsFilteredPagedAsync_ReturnsEventsOrderedByStartAtAndThenById() 
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;

            var FilterByFromSpec = new EventSearchSpecification();
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(2),
                EndAt = CurrentTime.AddHours(4),
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(4),
                EndAt = CurrentTime.AddHours(6),
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);
            //порядок ожидается такой : Event1->Event2->Event4->Event5->(Event3|Event6) последние два ивента по Id
            //для проверки порядка нам нужны поля StartAt и Id
            var expectedList = new List<Event> { Event1, Event2, Event4, Event5};
            if (Event3.Id < Event6.Id)
            {
                expectedList.AddRange(Event3,Event6);
            }
            else 
            {
                expectedList.AddRange(Event6, Event3);
            }

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert

            var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var actualList = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByFromSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(actualList);
            Assert.Equal(6, actualList.Count);
            //в C# DateTime.UtcNow и точность -- 7 знаков после запятой в секундах
            //В PostgreSQL timestamptz время хранится с точностью до микросекунд и точность --  6 знаков после запятой.
            Assert.Equal(expectedList.Select(e=>e.StartAt).ToList(), actualList.Select(e=>e.StartAt).ToList(), (expected, actual) => (expected - actual).Duration() <= TimeSpan.FromMicroseconds(1));
            Assert.Equal(expectedList.Select(e => e.Id).ToList(), actualList.Select(e => e.Id).ToList());


        }










        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации. В Критерии фильтрации заданы только page and pageSize
        //Проверка CountAsync
        [Theory]
        //pageSize and page
        [InlineData(1, 2)]
        [InlineData(2, 4)]//страница за пределами данных
        public async Task ListEventsFilteredPagedAsync_NotFilteredButPaged_PagedEvents(int pageSize, int page)
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;

            int pageSize_ = pageSize;
            int page_ = page;


            var Filter= new EventSearchSpecification(pageSize: pageSize_, page: page_);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(2),
                EndAt = CurrentTime.AddHours(4),
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(4),
                EndAt = CurrentTime.AddHours(6),
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);


            var ListOFEvents = new List<Event> { Event1, Event2, Event3, Event4, Event5, Event6 };
            var FilteredListOfEventsByTitle = ListOFEvents.AsQueryable();
            var FilteredListOfEventsByTitleOrderedByStartAtThenById = FilteredListOfEventsByTitle.OrderBy(e => e.StartAt).ThenBy(e => e.Id);
            var ExpectedResultList = FilteredListOfEventsByTitleOrderedByStartAtThenById.Skip((page_ - 1) * pageSize_).Take(pageSize_);


            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ActualResultList = await eventRepositoryAct.ListEventsFilteredPagedAsync(Filter, TestContext.Current.CancellationToken);
            Assert.Equal(ExpectedResultList.Select(e => e.Id).ToList(), ActualResultList.Select(e => e.Id).ToList());

            //Проверка CountAsync()
            //Arrange 
            int expectedCount = FilteredListOfEventsByTitleOrderedByStartAtThenById.Count();

            //act
            using var contextCountAsyncAct = CreateContext();
            var eventRepositoryCountAsyncAct = new EventRepository(contextCountAsyncAct);
            var actualCount = await eventRepositoryCountAsyncAct.CountAsync(Filter);
            Assert.Equal(expectedCount, actualCount);



        }


















        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации. Фильтрация по To, From and Title. фильтр такой : e.StartAt >= from.Value, e.EndAt <= to.Value
        // и Title
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByFromToTitle_FilteredEvents()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;
            DateTime TimeForFilteringFrom = CurrentTime.AddHours(2);
            DateTime TimeForFilteringTo = CurrentTime.AddHours(6);


            var FilterByFromSpec = new EventSearchSpecification(from: TimeForFilteringFrom, to: TimeForFilteringTo, title: TitleForSpec);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByFromSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(ListOfFilteredEvents);
            Assert.Single(ListOfFilteredEvents);
            Assert.All(ListOfFilteredEvents.Select(e => e.Title).ToList(), (e) => Assert.Equal("SHREK 1", e));
        }
















        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации. Фильтрация по From and Title. фильтр такой : e.StartAt >= from.Value
        // и Title
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByFromAndTitle_FilteredEvents()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;
            DateTime TimeForFilteringFrom = CurrentTime.AddHours(2);

            var FilterByFromSpec = new EventSearchSpecification(from: TimeForFilteringFrom, title: TitleForSpec);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByFromSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(ListOfFilteredEvents);
            Assert.Equal(2, ListOfFilteredEvents.Count);
            Assert.All(ListOfFilteredEvents.Select(e => e.Title).ToList(), (e) => Assert.Equal("SHREK 1", e));
        }











        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации. Фильтрация по To and Title. фильтр такой : e.EndAt <= to.Value
        // и Title
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByToAndTitle_FilteredEvents()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;
            DateTime TimeForFilteringTo = CurrentTime.AddHours(6);
         
            var FilterByFromSpec = new EventSearchSpecification(to: TimeForFilteringTo, title: TitleForSpec);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByFromSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(ListOfFilteredEvents);
            Assert.Equal(2, ListOfFilteredEvents.Count);
            Assert.All(ListOfFilteredEvents.Select(e => e.Title).ToList(), (e) => Assert.Equal("SHREK 1",e));
        }








        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации. Фильтрация по From and To. фильтр такой : e.StartAt >= from.Value
        // И e.EndAt <= to.Value
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByFromAndTo_FilteredEvents()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;
            DateTime TimeForFilteringFrom = CurrentTime.AddHours(2);
            DateTime TimeForFilteringTo = CurrentTime.AddHours(6);
            var FilterByFromSpec = new EventSearchSpecification(from: TimeForFilteringFrom, to: TimeForFilteringTo);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(1),
                EndAt = CurrentTime.AddHours(3),
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(3),
                EndAt = CurrentTime.AddHours(5),
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(5),
                EndAt = CurrentTime.AddHours(7),
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByFromSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(ListOfFilteredEvents);
            Assert.Equal(2, ListOfFilteredEvents.Count);
            Assert.All(ListOfFilteredEvents.Select(e=>e.Title).ToList(), (e) => Assert.Contains(e, new List<string> { "SPIDER-NOIR", "SHREK 1"}));
        }








        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации. Фильтрация по To
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByTo_FilteredEvents()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;
            DateTime TimeForFiltering = CurrentTime.AddHours(5);
            var FilterByFromSpec = new EventSearchSpecification(to: TimeForFiltering);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(2),
                EndAt = CurrentTime.AddHours(4),
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(2),
                EndAt = CurrentTime.AddHours(4),
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(2),
                EndAt = CurrentTime.AddHours(4),
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(4),
                EndAt = CurrentTime.AddHours(6),
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(4),
                EndAt = CurrentTime.AddHours(6),
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(4),
                EndAt = CurrentTime.AddHours(6),
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByFromSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(ListOfFilteredEvents);
            Assert.Equal(3, ListOfFilteredEvents.Count);
            Assert.All(ListOfFilteredEvents, (e) => Assert.Equal("SHREK 1", e.Title));
        }







        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации. Фильтрация по From
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByFrom_FilteredEvents()
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;
            DateTime TimeForFiltering = CurrentTime.AddHours(-3);
            var FilterByFromSpec = new EventSearchSpecification(from: TimeForFiltering);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(-4),
                EndAt = CurrentTime,
                TotalSeats = 50
            };


            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(-4),
                EndAt = CurrentTime,
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(-4),
                EndAt = CurrentTime,
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByFromSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(ListOfFilteredEvents);
            Assert.Equal(3, ListOfFilteredEvents.Count);
            Assert.All(ListOfFilteredEvents, (e) => Assert.Equal("SPIDER-NOIR", e.Title));
        }







        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации.
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByTitle_FilteredEvents() 
        {
            //Arrange
            await ResetDatabaseAsync();
            using var context = CreateContext();
            var eventRepository = new EventRepository(context);
            string TitleForSpec = "SHREK 1";
            DateTime CurrentTime = DateTime.UtcNow;
            var FilterByTitleSpec = new EventSearchSpecification(TitleForSpec);
            //Arrange
            //SHREK
            var EventDto1 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };
            
           
            var EventDto2 = new EventDto
            {
                Title = TitleForSpec,
                Description = "2D",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };
            var EventDto3 = new EventDto
            {
                Title = TitleForSpec,
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };


            //SPIDER-NOIR
            var EventDto4 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "2D",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };
            var EventDto5 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };
            var EventDto6 = new EventDto
            {
                Title = "SPIDER-NOIR",
                Description = "3D IMAX",
                StartAt = CurrentTime.AddHours(-2),
                EndAt = CurrentTime,
                TotalSeats = 50
            };

            var Event1 = Event.Create(EventDto1);
            var Event2 = Event.Create(EventDto2);
            var Event3 = Event.Create(EventDto3);
            var Event4 = Event.Create(EventDto4);
            var Event5 = Event.Create(EventDto5);
            var Event6 = Event.Create(EventDto6);

            await eventRepository.AddEventAsync(Event1, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event2, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event3, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event4, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event5, TestContext.Current.CancellationToken);
            await eventRepository.AddEventAsync(Event6, TestContext.Current.CancellationToken);

            await eventRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByTitleSpec, TestContext.Current.CancellationToken);
            Assert.NotEmpty(ListOfFilteredEvents);
            Assert.Equal(3, ListOfFilteredEvents.Count);
            Assert.All(ListOfFilteredEvents, (e) => Assert.Equal(TitleForSpec, e.Title));
        }






        //создаем объект спецификации EventSearchSpesicifation, в котором определим 
        //правила фильтрации
        [Fact]
        public async Task ListEventsFilteredPagedAsync_FilteringByTitleEmptyDB_EmptyList()
        {
            //Arrange
            await ResetDatabaseAsync();
            string TitleForSpec = "SHREK 1";
            var FilterByTitleSpec = new EventSearchSpecification(TitleForSpec);

            //act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var ListOfFilteredEvents = await eventRepositoryAct.ListEventsFilteredPagedAsync(FilterByTitleSpec, TestContext.Current.CancellationToken);
            Assert.Empty(ListOfFilteredEvents);

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

            //Act and assert
            using var contextAct = CreateContext();
            var eventRepositoryAct = new EventRepository(contextAct);
            var eventToDelete = await eventRepositoryAct.GetEventByIdAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            eventRepositoryAct.DeleteEvent(eventToDelete!);
            await Assert.ThrowsAsync<DbUpdateException>( () => eventRepositoryAct.SaveChangesAsync(TestContext.Current.CancellationToken));

            //Arrange
            using var contextArrange = CreateContext();
            var bookingRepository = new BookingRepository(contextArrange);
            var BookingToDelete1 = await bookingRepository.GetBookingByIdAsync(BookingObject1.Id, TestContext.Current.CancellationToken);
            var BookingToDelete2 = await bookingRepository.GetBookingByIdAsync(BookingObject2.Id, TestContext.Current.CancellationToken);
            await bookingRepository.DeleteBookingAsync(BookingToDelete1!, TestContext.Current.CancellationToken);
            await bookingRepository.DeleteBookingAsync(BookingToDelete2!, TestContext.Current.CancellationToken);
            await bookingRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            using var contextAct1 = CreateContext();
            var eventRepositoryAct1 = new EventRepository(contextAct1);
            eventToDelete = await eventRepositoryAct1.GetEventByIdAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            eventRepositoryAct1.DeleteEvent(eventToDelete!);
            await eventRepositoryAct1.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Assert
            using var contextAssert = CreateContext();
            var eventRepositoryAssert = new EventRepository(contextAssert);
            var result = await eventRepositoryAssert.IsExistInRepositoryAsync(EventObjext.Id, TestContext.Current.CancellationToken);
            Assert.False(result);

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
        // проверяет, что метод AddEventAsync добавляет объект существующего в БД события. При сохранении изменений в БД (SaveChangesAsync) выбрасывается исключение DbUpdateException, так как объект уже существует в БД.
        public async Task AddEventAsync_AddsExistentEvent_DbUpdateExceptionWhenSaveChagesAsyncInvoced() { 
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
            //нарушение правил таблицы  - это DbUpdateException 
            //пропажи строки при обновлении/удалении -- DbUpdateConcurrencyException.
            //INSERT INTO "Events" -- вызывает нарушение правилы уникальности Первичного Ключа
            //ошибку ограничений базы данных EF Core всегда оборачивает в базовый DbUpdateException
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
