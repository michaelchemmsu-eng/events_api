using project.Models;
using project.Services.EventSpecificator;

namespace project.Services.Specifications
{
    public class BookingSearchSpecification : Specification<Booking>
    {
        public BookingSearchSpecification(
            string? title,
            DateTime? from, 
            DateTime? to,
            int? page, 
            int? pageSize)
        {
            if (page.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page.Value);
            }
            if (pageSize.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize.Value);
            }

            Criteria = b =>
                //если title null или пустой, то не фильтруем по title
                (string.IsNullOrEmpty(title) || b.Event.Title.ToLower().Contains(title.ToLower())) &&
                (!from.HasValue || b.Event.StartAt >= from.Value) && //если HasValue true, то фильтруем по StartAt >= from.Value, иначе не фильтруем по StartAt
                (!to.HasValue || b.Event.EndAt <= to.Value);
            OrderBy = b => b.Event.StartAt;
            ThenBy = b => b.Event.Id; // это нужно, чтобы при одинаковых StartAt события сортировались по Id, иначе будет ошибка при пагинации
            ApplyPaging(checked((page!.Value - 1) * pageSize!.Value), pageSize!.Value);

        }
    }
}
