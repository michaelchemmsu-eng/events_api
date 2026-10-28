using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    /// <summary>
    /// 
    /// </summary>
    public record EventResponse
    {
        

        public Guid Id { get; set; }
       
        public string Title { get; init; }
        /// <summary>
        /// Описание события
        /// </summary>
        public string? Description { get; init; }
        /// <summary>
        /// дата начала события
        /// </summary>
      
        public DateTime StartAt { get; init; }
        /// <summary>
        /// дата окончания события
        /// </summary>
  
        public DateTime EndAt { get; init; }

        /// <summary>
        /// общее число мест на событии
        /// </summary>

        public int TotalSeats { get; set; }

        /// <summary>
        /// текущее количество свободных мест 
        /// </summary>
        //  при создании равно TotalSeats
        public int AvailableSeats { get; set; }
    }
}
