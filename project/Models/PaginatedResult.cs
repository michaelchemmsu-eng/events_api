using Microsoft.AspNetCore.Mvc;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class PaginatedResult
    {
        /// <summary>
        /// общее число событий
        /// </summary>
        [Required(ErrorMessage = "total event count is required")]
        public int totalEvents { get; set; }
        [Required(ErrorMessage = "event array is required")]
        public EventResponse[] eventArray { get; set; } = [];
        [Required(ErrorMessage = "current page is required")]
        public int currentPage { get; set; }
        [Required(ErrorMessage = "page size of current page is required")]
        public int pageSizeOfCurrentPage { get; set; }

        
    }
}
