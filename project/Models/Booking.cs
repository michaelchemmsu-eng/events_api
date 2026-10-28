namespace project.Models
{
    /// <summary>
    /// класс модели Booking
    /// </summary>
    public class Booking
    {
        /// <summary>
        /// идентификатор бронирования, генерируется системой при создании бронирования
        /// </summary>
        public Guid Id { get; set; } // система сама будет генерировать id при создании бронирования
        /// <summary>
        /// идентификатор события, к которому относится бронирование
        /// </summary>
        public Guid EventId { get; set; } // идентификатор события, к которому относится бронирование
        /// <summary>
        /// статус бронирования, по умолчанию Pending
        /// </summary>
        public BookingStatus Status { get; set; } = BookingStatus.Pending; // статус бронирования, по умолчанию Pending
        /// <summary>
        /// дата и время создания брони, по умолчанию текущая дата и время
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //дата и время создания брони;
        /// <summary>
        /// дата и время обработки брони, может быть null, если бронь еще не обработана
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// изменяет статут Pending объекта Booking на статут Confirmed
        /// </summary>
        public void Confirm() 
        {
            if (this.Status == BookingStatus.Pending)
                
            { 
                this.Status = BookingStatus.Confirmed;
                this.ProcessedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// изменяет статут Pending объекта Booking на статут Rejected
        /// </summary>
        public void Reject()
        {
            if (this.Status == BookingStatus.Pending)
                { this.Status = BookingStatus.Rejected;
                this.ProcessedAt = DateTime.UtcNow;
            }
        }

    }
    /// <summary>
    /// Pending — бронь создана, ожидает обработки;
    /// Confirmed — бронь подтверждена;
    /// ejected — бронь отклонена.
    /// </summary>
    public enum BookingStatus { Pending=1, Confirmed , Rejected }
}
