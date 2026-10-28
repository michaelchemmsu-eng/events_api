namespace project.Models
{
    public static class EventMappingExtensions
    {
        /// <summary>
        /// метод расширения Maps Event to EventResponse
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public static EventResponse ToEventResponse(this Event ev) => new ()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
            TotalSeats = ev.TotalSeats,
            AvailableSeats = ev.AvailableSeats
            
        };
    }
}
