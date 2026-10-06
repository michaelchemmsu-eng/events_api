namespace project.Models
{
    /// <summary>
    /// класс модели Booking
    /// </summary>
    public class Booking
    {
        public Guid Id { get; set; } // система сама будет генерировать id при создании бронирования
        public Guid EventId { get; set; } // идентификатор события, к которому относится бронирование
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

    }
    /// <summary>
    /// Pending — бронь создана, ожидает обработки;
    /// Confirmed — бронь подтверждена;
    /// ejected — бронь отклонена.
    /// </summary>
    public enum BookingStatus { Pending=1, Confirmed , Rejected }
}
