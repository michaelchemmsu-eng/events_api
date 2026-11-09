using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using project.DataAccess;
using project.Models;
using project.Repositories;
using project.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Test
{
    public class EventServiceTests
    {
        private readonly IServiceProvider _serviceProvider;
        public EventServiceTests()
        {
            // Создаем уникальное имя базы для тестового класса: одно имя базы данных на все тесты в этом классе, чтобы данные сохранялись между тестами
            var dbName = Guid.NewGuid().ToString();

            // Создаем чистую коллекцию зависимостей
            var services = new ServiceCollection();

            // Регистрируем InMemory базу данных с нашим dbName 
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));

            // Регистрируем репозитории и тестируемые сервисы
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<EventService>();

            // Строим провайдер
            _serviceProvider = services.BuildServiceProvider();
        }
        
        
        [Fact]
        public async Task CreateEvent_ShouldSaveEventToDatabase()
        {
            //Arrange
            // Создаем Scope как будто пришел один HTTP-запрос
            using var scope = _serviceProvider.CreateScope();

            // Достаем готовый сервис со всеми внедренными зависимостями
            var eventService = scope.ServiceProvider.GetRequiredService<EventService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var eventDto = new EventDto
            {
                Title = "Концерт",

                StartAt = DateTime.UtcNow.AddDays(-10),
                EndAt = DateTime.UtcNow.AddDays(-9),
                TotalSeats = 100
            };
            // Act
            await eventService.CreateEventAsync(eventDto);

            // Assert
            var savedEvent = await dbContext.Events.FirstOrDefaultAsync();
            Assert.NotNull(savedEvent);
            Assert.Equal("Концерт", savedEvent.Title);
        }
    }
}
