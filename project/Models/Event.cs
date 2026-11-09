using project.CustomExceptions;
using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    /// <summary>
    /// базовый класс модели
    /// </summary>
    public class Event
    {
        public Guid Id { get; set; }
        /// <summary>
        /// заголовок
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Описание события
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// дата начала события
        /// </summary>
        public DateTime StartAt { get; set; }
        /// <summary>
        /// дата окончания события
        /// </summary>
        public DateTime EndAt { get; set; }

        /// <summary>
        /// общее количество мест на событии, required
        /// </summary>
        public int TotalSeats { get; set; }

        /// <summary>
        /// текущее количество свободных мест 
        /// </summary>
        //  при создании равно TotalSeats
        public int AvailableSeats { get; set; }

        /// <summary>
        /// навигационное свойство
        /// </summary>
        public List<Booking> Bookings { get; set; } = new List<Booking>();


        //для EF Core при чтении данных из БД и последующей материализации
        private Event() 
        {
            Title = null!;
        }


        /// <summary>
        /// фабричный метод, EventDto - аргумент.
        /// </summary>
        /// <param name="eventDto"></param>
        /// <returns></returns>
        /// <exception cref="ValidationException"></exception>
        public static Event Create(EventDto eventDto)
        {
            if (!eventDto.TotalSeats.HasValue)
            {
                throw new ValidationException("Validation error: TotalSeats has null value");
            }
            else if (eventDto.TotalSeats!.Value <= 0) 
            {
               
                throw new ValidationException("Validation error: a value for Total Seats must be positive");
            }

            return new Event
            {
                Id = Guid.NewGuid(),
                Title = eventDto.Title!,//обязательное поле в модели EventDto
                Description = eventDto.Description,

                //StartAt and EvndAt после валидации модели EventDto -- всегда не null
                StartAt = eventDto.StartAt!.Value,
                EndAt = eventDto.EndAt!.Value,

                // поле TotalSeats (int? -- обязательное для создания в модели EventDto
                TotalSeats = eventDto.TotalSeats!.Value,
                // при создании равно TotalSeats
                AvailableSeats = eventDto.TotalSeats!.Value
            };
        }




        /// <summary>
        /// Метод для резервирования мест. Число мест для резервирования передается в параметр count типа int (по умолчанию 1).
        /// Уменьшает AvailableSeats на count и возвращает true, если места есть; возвращает false, если свободных мест недостаточно.
        /// </summary>
        /// <param name="count">число мест для запроса на резервирования. По умолчанию 1</param>
        /// <returns> возвращает false, если свободных мест недостаточно</returns>
        public void ReserveSeat()
        {
            if (AvailableSeats <= 0)
            {
                throw new NoAvailableSeatsException($"Нет доступных мест для события {this.Id}");
            }
            AvailableSeats--;
        }


        /// <summary>
        /// метод для освобождения мест при отклонении брони. параметр int count - число отклоненных мест, по умолчанию 1 
        /// </summary>
        /// <param name="count">число отклоненных мест. по умолчанию 1</param>

        public void ReleaseSeats(int count = 1) 
        {
            AvailableSeats += count;
        }

        /// <summary>
        /// обновить значения свойств Event из EventDto, если они не null. Используется в методе UpdateEventAsync сервиса EventService
        /// </summary>
        /// <param name="eventDto"></param>
        public void UpdateFromDto(EventDto eventDto) 
        {
            //получить все свойства EventDto через reflection и присвоить их значения свойствам Event, если они не null
            var eventDtoProperties = typeof(EventDto).GetProperties();
            //пройтись в цикле по всем propertyInfo EventDto и присвоить их значения свойствам Event, если они не null
            foreach (var property in eventDtoProperties)
            {
                var value = property.GetValue(eventDto);
                if (value != null)
                {
                    var eventProperty = typeof(Event).GetProperty(property.Name);
                    if (eventProperty != null)
                    {
                        eventProperty.SetValue(this, value);
                    }
                }
            }
        }



    }
}
