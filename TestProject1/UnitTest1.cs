using project.Models;
using project.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace TestProject1
{
    public class EventServiceTest
    {
        private project.Services.EventService _eventService = new();

        public EventServiceTest()
        { }




        [Fact]
        //тест: создание события
        //Метод_Состояние_ОжидаемыйРезультат
        public void CreateEvent_Event()
        {
            //Arrange
            //объект DTO, чтобы передать в функцию CreateEvent - необходимые данные для нового Event
            EventDto eventDto = new EventDto
            {
                Title = "Title1",
                Description = "Description1",
                StartAt = DateTime.Now.AddDays(-5),
                EndAt = DateTime.Now
            };
            //Act
            var res = _eventService.CreateEvent(eventDto);


            //Assert
            //метод возвращает не null
            Assert.NotNull(res);
            //Id не пуст
            Assert.NotEqual(Guid.Empty, res.Id);
            //проверка маппинга
            Assert.Equal(res.Title, eventDto.Title);
            Assert.Equal(res.Description, eventDto.Description);
            Assert.Equal(res.StartAt, eventDto.StartAt);
            Assert.Equal(res.EndAt, eventDto.EndAt);
            //событие сохранилось в словаре
            var eventFromService = _eventService.GetEventById(res.Id);
            Assert.NotNull(eventFromService);
            Assert.Equal(res.Id, eventFromService.Id);

        }




















        [Fact]
        //тест: получение всех событий;
        public void GetAllEvents_PaginatedResultNotFiltered()
        {
            //Arrange
            //индексы i: Title{i}, Description{i} для генерирования объектов EventDto
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;//число объектов Event to generate
            //создание и наполнение словаря объектами Event 
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                _eventService.CreateEvent(eventDto);
            }

            //Act
            //тест: проверка при "пустых" аргументах 
            int expectedPage = 1;
            int expectedPageSize = 10;
            var result = _eventService.GetAllEvents(null, null, null, expectedPage, expectedPageSize);

            //Assert
            Assert.NotNull(result);

            //reveal the fields of the result object
            int totalEvents = result.totalEvents;
            EventResponse [] eventArray = result.eventArray;
            int currentPage = result.currentPage;
            int pageSizeOfCurrentPage = result.pageSizeOfCurrentPage;

            //тест: проверка полей
            Assert.Equal(totalEvents, numberOfEvents);
            Assert.Equal(eventArray.Length, numberOfEvents);
            Assert.Equal(expectedPage, currentPage);
            Assert.Equal(expectedPageSize, pageSizeOfCurrentPage);

            

        }







        [Fact]
        //тест: получение всех событий с фильтрацией по названию;
        public void GetAllEvents_PaginatedResultFilteredByTitle()
        {
            //случай пустого Storage
            //Arrange
            string titleFilter = "Title1";

            //Act
            var paginatedResult = _eventService.GetAllEvents(titleFilter,null,null);
            var actualTotalEvents = paginatedResult.totalEvents;
            var actualEventArray = paginatedResult.eventArray;
            var actualCurrentPage = paginatedResult.currentPage;
            var actualPageSizeOfCurrentPage = paginatedResult.pageSizeOfCurrentPage;
            //Assert
            Assert.Equal(0, actualTotalEvents);
            Assert.Equal(0, actualEventArray.Length);
            Assert.Equal(1, actualCurrentPage);
            Assert.Equal(0, actualPageSizeOfCurrentPage);




            //случай непустого Storage
            //Arrange
            //индексы i: Title{i}, Description{i} для генерирования объектов EventDto
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;//число объектов Event to generate
            //создание и наполнение словаря объектами Event 
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                _eventService.CreateEvent(eventDto);
            }

            //Act
            //тест: проверка при "пустых" аргументах 
            int expectedPage = 1;
            int expectedPageSize = 10;

            //тест: проверка фильтрации по title
            //Arrange


            //Act
            var filteredResult = _eventService.GetAllEvents(titleFilter, null, null, expectedPage, expectedPageSize).eventArray;

            //Assert
            Assert.All(filteredResult, e => Assert.Equal(titleFilter, e.Title));


        }






        //тест: получение всех событий с фильтрацией по from и to;
        [Fact]
        public void GetAllEvents_PaginatedResultFilteredByFromAndTo()
        {
            // Arrange: хранилище изначально пустое
            var fromFilter = DateTime.UtcNow.AddDays(-5);
            var toFilter = DateTime.UtcNow.AddDays(5);

            // Act
            var result = _eventService.GetAllEvents(null, fromFilter, toFilter);

            // Assert: проверяем, что на пустом хранилище возвращаются нули
            Assert.Equal(0, result.totalEvents);
            Assert.Empty(result.eventArray);
            Assert.Equal(1, result.currentPage);
            Assert.Equal(0, result.pageSizeOfCurrentPage);




            // Arrange
            var baseDate = DateTime.UtcNow;

            // Границы нашего фильтра от +5 дней до +15 дней
            fromFilter = baseDate.AddDays(5);
            toFilter = baseDate.AddDays(15);
            
            // Событие 1 - слишком рано (StartAt раньше fromFilter)
            _eventService.CreateEvent(new EventDto
            {
                Title = "Early - Event 1",
                StartAt = baseDate.AddDays(1),
                EndAt = baseDate.AddDays(3)
            });
            // Событие 2 - внутри диапазона 
            _eventService.CreateEvent(new EventDto
            {
                Title = "Target - Event 2",
                StartAt = baseDate.AddDays(6),
                EndAt = baseDate.AddDays(8)
            });
            // Событие 3 - внутри диапазона 
            _eventService.CreateEvent(new EventDto
            {
                Title = "Target - Event 3",
                StartAt = baseDate.AddDays(7),
                EndAt = baseDate.AddDays(14)
            });

            // Событие 4 -  внутри диапазона 
            _eventService.CreateEvent(new EventDto
            {
                Title = "Target - Event 4",
                StartAt = fromFilter,
                EndAt = toFilter
            });

            // Событие 5 - слишком поздно (EndAt позже toFilter)
            _eventService.CreateEvent(new EventDto
            {
                Title = "Late Event",
                StartAt = baseDate.AddDays(10),
                EndAt = baseDate.AddDays(20) // Вылезает за toFilter (+15)
            });

            // Act
            result = _eventService.GetAllEvents(null, fromFilter, toFilter);

            //Assert
            //проверка ровно 3 события из 5 должны пройти фильтр
            Assert.Equal(3, result.totalEvents);
            Assert.Equal(3, result.eventArray.Length);
            // Проверяем, что попали именно нужные события по Titile
            var titles = result.eventArray.Select(e => e.Title).ToList();
            Assert.Contains("Target - Event 2", titles);
            Assert.Contains("Target - Event 3", titles);
            Assert.Contains("Target - Event 4", titles);
            // Проверяем, что лишние события НЕ попали
            Assert.DoesNotContain("Early - Event 1", titles);
            Assert.DoesNotContain("Late Event", titles);
            
            Assert.All(result.eventArray, e => Assert.True(e.StartAt >= fromFilter));
            Assert.All(result.eventArray, e => Assert.True(e.EndAt <= toFilter));

        }














        [Fact]
        //тест: получение события по id;

        public void GetEventById_Id_Event()
        {
            //Arrange
            //так же, как и выше
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                _eventService.CreateEvent(eventDto);
            }
            var Ids = _eventService.GetAllEvents(null, null, null).eventArray.Select(e => e.Id).ToArray();//массив Id from the Storage
            int idxOfEvent = 0;//check this Event, the index in the Array
            var expectedEventFromStorage = _eventService.GetAllEvents(null, null, null).eventArray[idxOfEvent];


            //Act
            var actualEventFromStorage = _eventService.GetEventById(Ids[idxOfEvent]);
            //Assert
            Assert.Equal(expectedEventFromStorage.Title, actualEventFromStorage.Title);
            Assert.Equal(expectedEventFromStorage.Id, actualEventFromStorage.Id);
            Assert.Equal(expectedEventFromStorage.StartAt, actualEventFromStorage.StartAt);
            Assert.Equal(expectedEventFromStorage.EndAt, actualEventFromStorage.EndAt);

        }


        [Fact]
        //тест: получение события по несуществующему id;
        public void GetEventById_NonExistingId_ThrowsKeyNotFoundException()
        {
            //случай с пустым Storage
            // Arrange
            Guid nonexistingId = Guid.NewGuid(); // Generate a new GUID that is not in the storage
            // Act and Assert
            Assert.Throws<KeyNotFoundException>(() => _eventService.GetEventById(nonexistingId));


            //случай с непустым Storage
            //Arrange
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                _eventService.CreateEvent(eventDto);
            }
            // Act and Assert
            Assert.Throws<KeyNotFoundException>(() => _eventService.GetEventById(nonexistingId));

        }


        [Fact]
        //обновление существующего события;
        public void UpdateEvent_IdAndEventDto_()
        {
            //обновлят Event асинхронно
            //если Event с переданным в функцию id нет, то throw exception
            //Arrange
            //так же, как выше
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                _eventService.CreateEvent(eventDto);
            }



            var Ids = _eventService.GetAllEvents(null, null, null).eventArray.Select(e => e.Id).ToArray();//массив Id элементов 
            int idxOfEventToModify = 0;//index of the element in the Array to modify
            //EventDto for modification of the element of idxOfEventToModify
            EventDto eventDtoToModify = new EventDto
            {
                Title = "Title123",
                Description = "Description123",
                StartAt = DateTime.Now.AddDays(-10),
                EndAt = DateTime.Now
            };


            //Act. Update an existing Event
            _eventService.UpdateEvent(Ids[idxOfEventToModify], eventDtoToModify);

            //Assert
            //проверка модифицированного Event
            var modifiedEvent = _eventService.GetAllEvents(null, null, null).eventArray[idxOfEventToModify];

            //Id не должна измениться
            Assert.Equal(modifiedEvent.Id, Ids[idxOfEventToModify]);
            //Свойства должны измениться
            Assert.Equal(modifiedEvent.Title, eventDtoToModify.Title);
            Assert.Equal(modifiedEvent.StartAt, eventDtoToModify.StartAt);
            Assert.Equal(modifiedEvent.Description, eventDtoToModify.Description);
            Assert.Equal(modifiedEvent.EndAt, eventDtoToModify.EndAt);


        }



        //обновление события с несуществующим ID
        [Fact]
        public void UpdateEventById_NonExistingId_ThrowsKeyNotFoundException() 
        {
            //случай с пустым Storage
            Guid nonexistingId = new Guid(); //to check the exception throwing
            //EventDto for modification of the element of idxOfEventToModify
            EventDto eventDtoToModify = new EventDto
            {
                Title = "Title123",
                Description = "Description123",
                StartAt = DateTime.Now.AddDays(-10),
                EndAt = DateTime.Now
            };

       
            Assert.Throws<KeyNotFoundException>(() => _eventService.UpdateEvent(nonexistingId, eventDtoToModify));



            //случай с непустым Storage
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                _eventService.CreateEvent(eventDto);
            }
            
            Assert.Throws<KeyNotFoundException>(() => _eventService.UpdateEvent(nonexistingId, eventDtoToModify));

        }


        [Fact]
        //удаление существующего события;
        public void DeleteExistingEvent_Guid_()

        {

            //фунция DeleteEvent (Guid) молча удаляет элемент с переданным Guid из Storage. Если этого Guid нет в storage, то exception
            //Arrange
            //так же, как выше
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                _eventService.CreateEvent(eventDto);
            }





            var Ids = _eventService.GetAllEvents(null, null, null).eventArray.Select(e => e.Id).ToArray();
            var IdxOfExistingEvent = 0;
            var IdOfExistingEvent = Ids[IdxOfExistingEvent];
            //Assert. Check the present of the existing Event in the Storage
            var existingEventById = _eventService.GetEventById(IdOfExistingEvent);
            //Act
            _eventService.DeleteEvent(IdOfExistingEvent);
            //Assert.
            Assert.Throws<KeyNotFoundException>(() => _eventService.DeleteEvent(IdOfExistingEvent));


        }


        //пагинация событий;
        [Fact]
        public void Paging_PageAndPageSize_PaginatedResult()
        {
            //Arrange
            //так же, как выше
            int[] subscripts = {
                1, 1, 1, 1,
                2, 2, 2,
                3, 3,
                4};
            int numberOfEvents = subscripts.Length;
            List<EventDto> listOfEventDto = new();
            for (int i = 0; i < numberOfEvents; i++)
            {
                EventDto eventDto = new EventDto
                {
                    Title = $"Title{subscripts[i]}",
                    Description = $"Description{subscripts[i]}",
                    StartAt = DateTime.Now.AddDays(-i),
                    EndAt = DateTime.Now
                };
                listOfEventDto.Add(eventDto);
                _eventService.CreateEvent(eventDto);
            }

            //expected results
            int page = 2;
            int pageSize = 3;
            var expectedEventDtos = listOfEventDto
                .OrderBy(e => e.StartAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            int numberOfElements = expectedEventDtos.Count();

            //Act
            var actualPaginatedResults = _eventService.GetAllEvents(null, null, null, page, pageSize);

            //Assert. Проверяем поля PaginatedResult объекта actualPaginatedResults: число элементов всего in storage, текущая страница, размер текущей страницы
            Assert.Equal(numberOfEvents, actualPaginatedResults.totalEvents);
            Assert.Equal(page, actualPaginatedResults.currentPage);
            Assert.Equal(pageSize, actualPaginatedResults.pageSizeOfCurrentPage);
            Assert.Equal(numberOfElements, actualPaginatedResults.eventArray.Count());

            //Assert.
            for (int i = 0; i < numberOfElements; i++)
            {
                string expectedTitle = expectedEventDtos[i].Title;
                string expectedDescription = expectedEventDtos[i].Description;
                DateTime expectedStartAt = expectedEventDtos[i].StartAt.Value;
                DateTime expectedEndAt = expectedEventDtos[i].EndAt.Value;

                string actualTitle = actualPaginatedResults.eventArray[i].Title;
                string actualDescription = actualPaginatedResults.eventArray[i].Description;
                DateTime actualStartAt = actualPaginatedResults.eventArray[i].StartAt;
                DateTime actualEndAt = actualPaginatedResults.eventArray[i].EndAt;

                Assert.Equal(expectedTitle, actualTitle);
                Assert.Equal(expectedDescription, actualDescription);
                Assert.Equal(expectedStartAt, actualStartAt);
                Assert.Equal(expectedEndAt, actualEndAt);

            }

        }


        //комбинированная фильтрация.
        [Fact]
        public void CombinedFiltering_TitleAndFromAndTo_PaginatedResult()
        {
            //Arrange 
            var baseDate = DateTime.UtcNow;

            //диапазон поиска
            string titleFilter = "Meetup";
            DateTime fromFilter = baseDate.AddDays(5);
            DateTime toFilter = baseDate.AddDays(15);

            // Событие 1 - в диапазоне, название содержит "Meetup" -> должно пройти фильтр
            _eventService.CreateEvent(new EventDto
            {
                Title = "DotNet - Meetup - Spring",
                StartAt = baseDate.AddDays(6),
                EndAt = baseDate.AddDays(8)
            });

            // Событие 2 - в диапазоне и название содержит "Meetup" -> должно пройти фильтр
            _eventService.CreateEvent(new EventDto
            {
                Title = "C# Meetup Online",
                StartAt = baseDate.AddDays(7),
                EndAt = baseDate.AddDays(14)
            });

            // Событие 3 - содержит "Meetup", но дата слишком ранняя -> отсеять
            _eventService.CreateEvent(new EventDto
            {
                Title = "Old Meetup",
                StartAt = baseDate.AddDays(1),
                EndAt = baseDate.AddDays(3)
            });

            // Событие 4 - название подходит, но дата слишком поздняя -> отсеять
            _eventService.CreateEvent(new EventDto
            {
                Title = "Future Meetup",
                StartAt = baseDate.AddDays(10),
                EndAt = baseDate.AddDays(25) // Вылезает за toFilter (+15)
            });

            // Событие 5 - даты подходят, но название не содержит "Meetup" -> отсеять
            _eventService.CreateEvent(new EventDto
            {
                Title = "Java Conference", 
                StartAt = baseDate.AddDays(7),
                EndAt = baseDate.AddDays(10)
            });

            // Событие 6 - даты и название не подходят -> отсеять
            _eventService.CreateEvent(new EventDto
            {
                Title = "Python Course",
                StartAt = baseDate.AddDays(1),
                EndAt = baseDate.AddDays(30)
            });

            // Act
            var result = _eventService.GetAllEvents(titleFilter, fromFilter, toFilter);

            // Assert (Проверки)

            //ровно 2 события из 6 должны пройти комбинированный фильтр!
            Assert.Equal(2, result.totalEvents);
            Assert.Equal(2, result.eventArray.Length);

            // Проверяем, что вернулись именно те два события:
            var returnedTitles = result.eventArray.Select(e => e.Title).ToList();
            Assert.Contains("DotNet - Meetup - Spring", returnedTitles);
            Assert.Contains("C# Meetup Online", returnedTitles);

            // Проверяем, что отсеянные события точно не попали:
            Assert.DoesNotContain("Old Meetup", returnedTitles);
            Assert.DoesNotContain("Java Conference", returnedTitles);
            Assert.DoesNotContain("Python Course", returnedTitles);
            Assert.DoesNotContain("Future Meetup", returnedTitles);



            // Проверяем свойства каждого возвращенного элемента:
            Assert.All(result.eventArray, e =>
                                        {
                                            // Название содержит искомое слово без учета регистра
                                            Assert.Contains(titleFilter, e.Title, StringComparison.OrdinalIgnoreCase);
                                            // Даты лежат в диапазоне
                                            Assert.True(e.StartAt >= fromFilter, "StartAt должен быть >= fromFilter");
                                            Assert.True(e.EndAt <= toFilter, "EndAt должен быть <= toFilter");
                                        }
            );
        }



        //обновление события с некорректными датами (EndAt раньше StartAt).
        //это валидация модели
        [Fact]
        public void ValidateEventDto_EndAtBeforeStartAt_Fail()
        {
            //Arrange. Dto с кривыми данными
            var eventDtoIncorret = new EventDto
            {
                Title = "Title1",
                Description = "Desciption1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(-5)
            };
            var validationContext = new ValidationContext(eventDtoIncorret);
            var validationResults = eventDtoIncorret.Validate(validationContext).ToList();
            Assert.Contains(nameof(eventDtoIncorret.EndAt), validationResults[0].MemberNames);
        }



        //тесты на некорректные значения page и pageSize в GetAllEvents
        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        public void GetAllEvents_InvalidPage_ThrowsArgumentOutOfRangeException(int badPage, int pageSize)
        {
            var service = new EventService();
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetAllEvents(null, null, null, badPage, pageSize));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, -5)]
        [InlineData(1, 101)]
        public void GetAllEvents_InvalidPageSize_ThrowsArgumentOutOfRangeException(int page, int badPageSize)
        {
            var service = new EventService();
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetAllEvents(null, null, null, page, badPageSize));
        }

    }
}
