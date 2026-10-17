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
     
        public int totalEvents { get; set; }
        /// <summary>
        /// массив EventResponse на странице результата
        /// </summary>
        public EventResponse[] eventArray { get; set; } = [];
        /// <summary>
        /// номер страницы
        /// </summary>
        public int currentPage { get; set; }
        /// <summary>
        /// размер страницы
        /// </summary>
        public int pageSizeOfCurrentPage { get; set; }

        
    }
}
