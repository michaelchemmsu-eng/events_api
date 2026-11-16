using project.Models;
using project.Services.EventSpecificator;
namespace project.Services.Specifications
{
    public class PendingBookingsSpecification: Specification<Booking>
    {
        public PendingBookingsSpecification()
        {
            Criteria = b => b.Status == BookingStatus.Pending;
        }
    }

}
