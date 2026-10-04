namespace project.Models
{
    public static class EventMappingExtensions
    {
        public static EventResponse ToEventResponse(this Event ev) => new ()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt
        };
    }
}
