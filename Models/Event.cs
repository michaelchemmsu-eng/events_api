namespace project.Models
{
    /// <summary>
    /// базовый класс модели
    /// </summary>
    public class Event
    {
        public Guid Id { get; set; }
        /// <summary>
        /// заголовок
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Описание события
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// дата начала события
        /// </summary>
        public DateTime StartAt { get; set; }
        /// <summary>
        /// дата окончания события
        /// </summary>
        public DateTime EndAt { get; set; }
    }
}
