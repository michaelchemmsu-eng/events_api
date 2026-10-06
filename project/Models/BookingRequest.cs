using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    /// <summary>
    /// класс модели Booking
    /// </summary>
    public class BookingRequest: IValidatableObject
    {
        [Required (ErrorMessage ="Event Id is required")]
        public Guid EventId { get; set; } // идентификатор события, к которому относится бронирование
        [Required(ErrorMessage = "Event Status is required")]
        public BookingStatus Status { get; set; }
        [Required(ErrorMessage = "Event Creation Time is required")]        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.Date; //дата и время создания брони;
        //это должно быть >= CreatedAt, если не null
        public DateTime? ProcessedAt { get; set; }//дата и время обработки брони.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ProcessedAt.HasValue && ProcessedAt.Value >= CreatedAt) 
            {
                yield return new ValidationResult(
                 "Дата и время создания брони должна быть меньше даты и времени обработки брони",
                 new[] { nameof(CreatedAt), nameof(ProcessedAt) });
            }
        }

    }

}
