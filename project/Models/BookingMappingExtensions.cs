namespace project.Models
{
    public static class BookingMappingExtensions
    {
        public static BookingResponse ToBookingResponse(this Booking b) => new()
        {
            BookingId = b.Id,
            EventId = b.EventId,
            Status = b.Status
        };
    }
}
