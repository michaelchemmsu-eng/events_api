using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace project.Models
{
    /// <summary>
    /// базовый класс модели Event
    /// </summary>

    public class EventDto: IValidatableObject
    {

        //public Guid Id { get; set; } система сама будет генерировать id при создании события
        /// <summary>
        /// заголовок
        /// </summary>
        [Required (ErrorMessage = "Название события обязательно")]
        public string Title { get; set; }
        /// <summary>
        /// Описание события
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// дата начала события
        /// </summary>
        [Required (ErrorMessage = "Дата начала события обязательно")]
        public DateTime StartAt { get; set; }
        /// <summary>
        /// дата окончания события
        /// </summary>
        [Required (ErrorMessage = "Дата окончания события обязательно")]
        public DateTime EndAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartAt >= EndAt)
            {
                yield return new ValidationResult(
                    "Дата начала события должна быть меньше даты окончания события",
                    new[] { nameof(StartAt), nameof(EndAt) });
            }
        }
    }
}
