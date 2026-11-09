using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace project.Models
{
    /// <summary>
    /// базовый класс модели EventDto
    /// </summary>

    public class EventDto: IValidatableObject
    {

        //public Guid Id { get; set; } система сама будет генерировать id при создании события
        /// <summary>
        /// заголовок
        /// </summary>
        [Required (ErrorMessage = "Название события обязательно")]
        public string? Title { get; set; }
        /// <summary>
        /// Описание события
        /// </summary>
        // необязательное поле
        public string? Description { get; set; }
        /// <summary>
        /// дата начала события
        /// </summary>
        [Required (ErrorMessage = "Дата начала события обязательно")]
        public DateTime? StartAt { get; set; }//nullable value. дефолтное значение : hasvalue = false 
        /// <summary>
        /// дата окончания события
        /// </summary>
        [Required (ErrorMessage = "Дата окончания события обязательно")]
        public DateTime? EndAt { get; set; }//nullable value. дефолтное значение : hasvalue = false 

        /// <summary>
        /// общее количество мест на событии, required
        /// </summary>
        [Required (ErrorMessage = "Необходимо указать общее количество мест на событии")]
        //[Range(0,int.MaxValue,ErrorMessage = "Количество мест должно быть больше нуля")] валидируется в Event
        //почему тип int? , а не int 
        public int? TotalSeats { get; set; }

        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartAt.HasValue && EndAt.HasValue && StartAt >= EndAt)
            {
                yield return new ValidationResult(
                    "Дата начала события должна быть меньше даты окончания события",
                    new[] { nameof(StartAt), nameof(EndAt) });
            }
        }
    }
}
