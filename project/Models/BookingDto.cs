namespace project.Models
{

    //при обновлении бронирования в IBookingRepository в хранилище передать этот тип
    /// <summary>
    /// класс модели BookingDto, который используется для передачи данных о бронировании при обновлении в хранилище
    /// </summary>
    public class BookingDto
    {
        /// <summary>
        /// статус , на который изменяется бронирование
        /// </summary>
        public BookingStatus Status { get; set; }
        /// <summary>
        /// дата и время обработки брони, может быть null, если бронь еще не обработана
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

    }

}
