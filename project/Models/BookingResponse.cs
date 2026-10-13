namespace project.Models
{
    /// <summary>
    /// класс модели Booking
    /// </summary>
    public class BookingResponse
    {
        /// <summary>
        /// идентификатор бронирования
        /// </summary>
        public Guid BookingId { get; set; }
        /// <summary>
        /// идентификатор события, к которому относится бронирование в ответе
        /// </summary>
        public Guid EventId { get; set; }
        /// <summary>
        /// статус бронирования в ответе
        /// </summary>
        public BookingStatus Status { get; set; }

    }
}

