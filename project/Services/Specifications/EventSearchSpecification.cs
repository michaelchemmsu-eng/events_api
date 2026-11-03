using System.Linq.Expressions;
using project.Services.EventSpecificator;
using project.Models;

namespace project.Services.Specifications
{
    /// <summary>
    /// Спецификация фильтрации: по заголовку, если задан
    /// по дате начала , если задана
    /// по дате окончания, если задана
    /// Результат пагинирован, если заданы критерии
    /// </summary>
    public class EventSearchSpecification: Specification<Event>
    {
        public EventSearchSpecification(
            string? title,
            DateTime? from, DateTime? to,
            int? page, int? pageSize)
        
        
        {
            if (page.HasValue && pageSize.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page.Value);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize.Value);
            }
            //если title null или пустой, то не фильтруем по title 
            Criteria = e => (string.IsNullOrEmpty(title) || e.Title.ToLower().Contains(title.ToLower())) &&
                (!from.HasValue || e.StartAt >= from.Value) && //если HasValue true, то фильтруем по StartAt >= from.Value, иначе не фильтруем по StartAt
                (!to.HasValue || e.EndAt <= to.Value);
            OrderBy = e => e.StartAt;
            ThenBy = e => e.Id; // это нужно, чтобы при одинаковых StartAt события сортировались по Id, иначе будет ошибка при пагинации
            ApplyPaging(checked((page!.Value - 1) * pageSize!.Value), pageSize!.Value);
        }
    }

}
