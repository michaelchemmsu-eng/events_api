using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    /// <summary>
    /// 
    /// </summary>
    public record EventResponse
    {
        [Required(ErrorMessage = "ID события обязательно")]

        public Guid Id { get; set; }
        [Required(ErrorMessage = "Название события обязательно")]
        public string Title { get; init; }
        /// <summary>
        /// Описание события
        /// </summary>
        public string? Description { get; init; }
        /// <summary>
        /// дата начала события
        /// </summary>
        [Required(ErrorMessage = "Дата начала события обязательно")]
        public DateTime StartAt { get; init; }
        /// <summary>
        /// дата окончания события
        /// </summary>
        [Required(ErrorMessage = "Дата окончания события обязательно")]
        public DateTime EndAt { get; init; }
    }
}
