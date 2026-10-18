using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Bson;
using project.CustomExceptions;
using project.Models;
using project.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
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
                        EndAt = DateTime.UtcNow.AddDays(3),
                        TotalSeats = 15
                   
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
                        EndAt = DateTime.UtcNow.AddDays(3),
                        TotalSeats = 15
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
                        EndAt = DateTime.UtcNow.AddDays(3),
                        TotalSeats = 15
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
                        EndAt = DateTime.UtcNow.AddDays(3),
                        TotalSeats = 15
                        
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
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                async () =>
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
                        EndAt = DateTime.UtcNow.AddDays(3),
                        TotalSeats = 15
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
                        EndAt = DateTime.UtcNow.AddDays(3),
                        TotalSeats = 15
                    }
                );
            Guid NonExistingBookingId = Guid.NewGuid();
            //Act & Assert
            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _bookingService.GetBookingByIdAsync(NonExistingBookingId); ;
            });
        }








        //Создание брони уменьшает AvailableSeats на 1.
        [Fact]
        public async Task CreateBooking_LowersAvailableSeatsByOne()
        {
            //Arrange
            var eventResponse = _eventService.CreateEvent
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
            var eventResponseId = eventResponse.Id;
            //Act
            var bookingResponseObject = await _bookingService.CreateBookingAsync(eventResponseId);
            var eventAfterCreatBooking = _eventService.GetEventResponseById(eventResponseId);
            //Assert
            Assert.Equal(1, eventAfterCreatBooking.AvailableSeats);
         

        }









        //После исчерпания мест следующая попытка выбрасывает NoAvailableSeatsException.
        [Fact]
        public async Task CreateBookingOfTheEventWithNoAvailableSeats_ThrowsNoAvailableSeatsException()
        {
            //Arrange
            var eventResponse = _eventService.CreateEvent
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
            var bookingResponseObject = await _bookingService.CreateBookingAsync(eventResponseId);
            //Act & Assert
            NoAvailableSeatsException exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventResponseId);
            });


        }









        //Создание нескольких броней (до лимита) — все успешны, у каждой уникальный Id.
        [Fact]
        public async Task CreatedBookingsUntilAvailbaleSeatsPresent_NoExceptionsAndUniqueIds() 
        {
            //Arrange
            int Nseats = 3;
            var eventResponse = _eventService.CreateEvent
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
            var tasks = new List<Task<BookingResponse>>();
            
            //Act
            for (int i = 0; i < Nseats; i++) 
            {
                var task = _bookingService.CreateBookingAsync(eventResponse.Id);
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

        }




        //Бронирование при отсутствии мест → NoAvailableSeatsException.
        [Fact]
        public async Task BookingEventWithNoAvailableSeats_ThrowNoAvailableSeatsException() 
        {
            //Arrange
            var eventResponse = _eventService.CreateEvent
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
            await _bookingService.CreateBookingAsync(eventResponse.Id);
            var eventResponseId = eventResponse.Id;
            //Act & Assert
            NoAvailableSeatsException exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventResponseId);
            });

        }




        //тесты на смену статуса брони:
        //После вызова Confirm() бронь возвращает статус Confirmed и заполненный ProcessedAt.
        [Fact]
        public async Task ConfirmBooking_ChangedBookingStatus() 
        {
            //Arrange
            var eventResponse = _eventService.CreateEvent
               (
                   new EventDto
                   {
                       Title = "Test Event 1",
                       Description = "Test Description 1",
                       StartAt = DateTime.UtcNow.AddDays(1),
                       EndAt = DateTime.UtcNow.AddDays(3),
                       TotalSeats = 12
                   }
               );
            await _bookingService.CreateBookingAsync(eventResponse.Id);

            eventResponse = _eventService.CreateEvent
               (
                   new EventDto
                   {
                       Title = "Test Event 2",
                       Description = "Test Description 2",
                       StartAt = DateTime.UtcNow.AddDays(1),
                       EndAt = DateTime.UtcNow.AddDays(3),
                       TotalSeats = 10
                   }
               );
            await _bookingService.CreateBookingAsync(eventResponse.Id);

            //получить все pending-букинги 
            var pendingBookingsIEnumerable = _bookingRepository.GetPending();

            var pendingBookingObjectsList = pendingBookingsIEnumerable.ToList();
            var pendingBookingsIds = pendingBookingObjectsList.Select(b => b.Id).ToList();
            //Act
            // изменить объект, обновить в репозитории
            foreach (var pendingBookingObject in pendingBookingObjectsList)
            {
                pendingBookingObject.Confirm();
                var updatedBookingDto = new BookingDto 
                {
                    Status = pendingBookingObject.Status,
                    ProcessedAt = pendingBookingObject.ProcessedAt
                };
                _bookingRepository.UpdateBooking(pendingBookingObject.Id, updatedBookingDto);
            }
            //Assert
            var ConfirmedBookingIds = _bookingRepository.GetAllBookings().Where(b => b.Status == BookingStatus.Confirmed).Select(b => b.BookingId).ToList();
            //Id collection of pendong booking and Id collection of confirmed booking are equal
            Assert.Equal(pendingBookingsIds.Count, ConfirmedBookingIds.Count);
            Assert.All(pendingBookingsIds, (Id) => { Assert.Contains(Id, ConfirmedBookingIds);});

        }








        //тесты на смену статуса брони:
        //После вызова Reject() бронь возвращает статус Rejected и заполненный ProcessedAt.
        [Fact]
        public async Task RejectBooking_ChangedBookingStatus()
        {
            //Arrange
            var eventResponse = _eventService.CreateEvent
               (
                   new EventDto
                   {
                       Title = "Test Event 1",
                       Description = "Test Description 1",
                       StartAt = DateTime.UtcNow.AddDays(1),
                       EndAt = DateTime.UtcNow.AddDays(3),
                       TotalSeats = 12
                   }
               );
            await _bookingService.CreateBookingAsync(eventResponse.Id);

            eventResponse = _eventService.CreateEvent
               (
                   new EventDto
                   {
                       Title = "Test Event 2",
                       Description = "Test Description 2",
                       StartAt = DateTime.UtcNow.AddDays(1),
                       EndAt = DateTime.UtcNow.AddDays(3),
                       TotalSeats = 10
                   }
               );
            await _bookingService.CreateBookingAsync(eventResponse.Id);

            //получить все pending-букинги 
            var pendingBookingsIEnumerable = _bookingRepository.GetPending();

            var pendingBookingObjectsList = pendingBookingsIEnumerable.ToList();
            var pendingBookingsIds = pendingBookingObjectsList.Select(b => b.Id).ToList();
            //Act
            // изменить объект, обновить в репозитории
            foreach (var ChangedBookingObject in pendingBookingObjectsList)
            {
                ChangedBookingObject.Reject();
                var updatedBookingDto = new BookingDto
                {
                    Status = ChangedBookingObject.Status,
                    ProcessedAt = ChangedBookingObject.ProcessedAt
                };
                _bookingRepository.UpdateBooking(ChangedBookingObject.Id, updatedBookingDto);
            }
            //Assert
            var RejectedBookingIds = _bookingRepository.GetAllBookings().Where(b => b.Status == BookingStatus.Rejected).Select(b => b.BookingId).ToList();
            //Id collection of pendong booking and Id collection of confirmed booking are equal
            Assert.Equal(pendingBookingsIds.Count, RejectedBookingIds.Count);
            Assert.All(pendingBookingsIds, (Id) => { Assert.Contains(Id, RejectedBookingIds); });

        }







        //После Reject() -> ReleaseSeats() -> количество свободных мест восстанавливается.
        [Fact]
        public async Task RejectAndReleaseSeats_RestoreSeats() 
        {
            //Arrange
            var eventResponse1 = _eventService.CreateEvent
               (
                   new EventDto
                   {
                       Title = "Test Event 1",
                       Description = "Test Description 1",
                       StartAt = DateTime.UtcNow.AddDays(1),
                       EndAt = DateTime.UtcNow.AddDays(3),
                       TotalSeats = 12
                   }
               );

            var eventResponse2 = _eventService.CreateEvent
              (
                  new EventDto
                  {
                      Title = "Test Event 2",
                      Description = "Test Description 2",
                      StartAt = DateTime.UtcNow.AddDays(1),
                      EndAt = DateTime.UtcNow.AddDays(3),
                      TotalSeats = 10
                  }
              );

            var EventAvailableSeatsArray = _eventService
               .GetAllEvents(null, null, null)
               .eventArray
               .Select(ev => ev.AvailableSeats)
               .ToArray();

            await _bookingService.CreateBookingAsync(eventResponse1.Id);

            await _bookingService.CreateBookingAsync(eventResponse2.Id);
            //получить все pending-букинги 
            var pendingBookingsIEnumerable = _bookingRepository.GetPending();

            var pendingBookingObjectsList = pendingBookingsIEnumerable.ToList();

           
          
            //Act
           
            foreach (var ChangedBookingObject in pendingBookingObjectsList)
            {
                ChangedBookingObject.Reject();
                var updatedBookingDto = new BookingDto
                {
                    Status = ChangedBookingObject.Status,
                    ProcessedAt = ChangedBookingObject.ProcessedAt
                };
                _bookingRepository.UpdateBooking(ChangedBookingObject.Id, updatedBookingDto);

                //Events: найти, изменить, сохранить
                var Event = _eventService.GetEventById(ChangedBookingObject.EventId);
                Event.ReleaseSeats();
                _eventService.UpdateEventById(Event.Id, Event);
            }

            //Assert
            //проверим свободные места
            var EventAvailableSeatsArrayAfterRejectedAndReleaseSeats = _eventService
                .GetAllEvents(null, null, null)
                .eventArray
                .Select(ev => ev.AvailableSeats)
                .ToArray();
            Assert.Equal(EventAvailableSeatsArray.Length, EventAvailableSeatsArrayAfterRejectedAndReleaseSeats.Length);
            Assert.All(EventAvailableSeatsArrayAfterRejectedAndReleaseSeats, 
                        (AvailableSeats) => { Assert.Contains(AvailableSeats, EventAvailableSeatsArray); });


        }














        //После Reject() -> ReleaseSeats() -> можно успешно создать новую бронь на то же место.
        [Fact]
        public async Task RejectAndReleaseSeats_BookingAvailbleForRestoredSeats()
        {
            //Arrange
            var eventResponse1 = _eventService.CreateEvent
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

            var eventResponse2 = _eventService.CreateEvent
              (
                  new EventDto
                  {
                      Title = "Test Event 2",
                      Description = "Test Description 2",
                      StartAt = DateTime.UtcNow.AddDays(1),
                      EndAt = DateTime.UtcNow.AddDays(3),
                      TotalSeats = 1
                  }
              );

            var EventAvailableSeatsArray = _eventService
               .GetAllEvents(null, null, null)
               .eventArray
               .Select(ev => ev.AvailableSeats)
               .ToArray();

            await _bookingService.CreateBookingAsync(eventResponse1.Id);

            await _bookingService.CreateBookingAsync(eventResponse2.Id);
            //получить все pending-букинги 
            var pendingBookingsIEnumerable = _bookingRepository.GetPending();

            var pendingBookingObjectsList = pendingBookingsIEnumerable.ToList();



            //Act
            var ReBookingTasks = new List<Task<BookingResponse>>();
            foreach (var ChangedBookingObject in pendingBookingObjectsList)
            {
                ChangedBookingObject.Reject();
                var updatedBookingDto = new BookingDto
                {
                    Status = ChangedBookingObject.Status,
                    ProcessedAt = ChangedBookingObject.ProcessedAt
                };
                _bookingRepository.UpdateBooking(ChangedBookingObject.Id, updatedBookingDto);

                //Events: найти, изменить, сохранить
                var Event = _eventService.GetEventById(ChangedBookingObject.EventId);
                Event.ReleaseSeats();
                _eventService.UpdateEventById(Event.Id, Event);

                //Снова забронируем
                ReBookingTasks.Add(_bookingService.CreateBookingAsync(Event.Id));
            }

            BookingResponse[] bookingResponses = await Task.WhenAll(ReBookingTasks);
            //Assert
            Assert.True(ReBookingTasks.All(t => t.IsCompletedSuccessfully));



        }







        //тесты на конкурентность.
        //Тест на защиту от овербукинга
        [Theory]
        [InlineData(1)]   // Тест запустится для 1 места
        [InlineData(5)]   // Тест запустится для 5 мест
        [InlineData(50)]  // Тест запустится для 50 мест
        public async Task OverbookingPreventionTest_SuccededAndUnSuccededQueries(int totalSeats) 
        {
            //Arrange
            int ExtraQueries = 10;
            int NQueries = totalSeats + ExtraQueries ;
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
                TaskList.Add(Task.Run(() => _bookingService.CreateBookingAsync(eventResponse.Id)));
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
            //подсчет успешных тасков
            int successCount = TaskList.Count(t => t.IsCompletedSuccessfully);
            Assert.Equal(totalSeats, successCount);

            // Считаем упавшие таски
            int failedCount = TaskList.Count(t => t.IsFaulted);
            Assert.Equal(ExtraQueries, failedCount);

            //проверяем что Falted tasks threw NoAvailableSeatsException 
            var failedTasks = TaskList.Where(t => t.IsFaulted);
            foreach (var task in failedTasks)
            {
                Assert.IsType<NoAvailableSeatsException>(task.Exception?.InnerException);
            }


            // Проверяем состояние события в хранилище, то есть свободных мест должно остаться 0
            var updatedEvent = _eventService.GetEventById(eventResponse.Id);
            Assert.Equal(0, updatedEvent.AvailableSeats);


        }




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
                TotalSeats = 0
            };

            // Act and Assert: проверяем, что выбрасывается ValidationException 
            Assert.Throws<ValidationException>(() =>_eventService.CreateEvent(dtoWithZeroSeats));
            Assert.Throws<ValidationException>(() => _eventService.CreateEvent(dtoWithNegativeValueSeats));

        }


    }


}
