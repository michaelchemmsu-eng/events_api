using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    /// <summary>
    /// класс модели Booking
    /// </summary>
    public class BookingRequest
    {
        //при создании бронирования , клиент должен указать только идетификатор события
        /// <summary>
        /// идентификатор события, к которому относится бронирование в запросе
        /// </summary>
        [Required (ErrorMessage ="Event Id is required")]
        public Guid EventId { get; set; } // идентификатор события, к которому относится бронирование

    }

}
