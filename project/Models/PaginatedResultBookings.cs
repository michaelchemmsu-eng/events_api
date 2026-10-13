using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class PaginatedResultBookings
    {
        public int totalBookings { get; set; }
      
        public BookingResponse[] BookingsArray { get; set; } = [];
      
        public int currentPage { get; set; }
      
        public int pageSizeOfCurrentPage { get; set; }
    }
}
