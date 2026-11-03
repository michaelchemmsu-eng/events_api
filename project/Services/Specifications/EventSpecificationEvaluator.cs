using project.Models;
using Microsoft.EntityFrameworkCore;
namespace project.Services.EventSpecificator
{
    /// <summary>
    /// Этот вспомогательный класс берет IQueryable от базы данных и навешивает на него правила из спецификации:
    /// </summary>
    public class SpecificationEvaluator
    {
        public static IQueryable<T> GetQuery<T>(IQueryable<T> query, Specification<T> specification, bool criteriaOnly = false) where T : class
        {
            if (specification.Criteria is not null)
            {
                query = query.Where(specification.Criteria);
            }

            if (criteriaOnly)
            {
                return query;
            }

            foreach (var include in specification.Includes)
            {
                query = query.Include(include);//Include в пространстве имен Microsoft.EntityFrameworkCore, который позволяет включать связанные сущности в запрос. 
            }

            foreach (var includeString in specification.IncludeStrings)
            {
                query = query.Include(includeString);
            }

            // Проверяем, что не заданы одновременно OrderBy и OrderByDescending
            if (specification.OrderBy is not null && specification.OrderByDescending is not null)
            {
                throw new InvalidOperationException("Choose one primary ordering.");
            }



            //если спецификация содержит OrderBy, то применяем его к query,
            //иначе проверяем наличие OrderByDescending и применяем его, если он есть.
            //Если оба отсутствуют, ordered будет null.
            IOrderedQueryable<T>? ordered = specification.OrderBy is not null ? 
                                                        query.OrderBy(specification.OrderBy) : 
                                                            specification.OrderByDescending is not null ?
                                                                query.OrderByDescending(specification.OrderByDescending) : null;
            // Если ThenBy задан, то проверяем, что уже есть первичный порядок (ordered не null), и применяем ThenBy к ordered.
            if (specification.ThenBy is not null)
            {
                if (ordered is null)
                    throw new InvalidOperationException("ThenBy requires a primary ordering.");

                ordered = ordered.ThenBy(specification.ThenBy);
            }

            // Если ordered не null, то присваиваем query ordered, иначе оставляем query без изменений.
            query = ordered ?? query; 
            if (specification.IsPagingEnabled)
            {
                if (ordered is null)
                    throw new InvalidOperationException("Paging requires an ordering.");

                query = query.Skip(specification.Skip!.Value)
                             .Take(specification.Take!.Value);
            }
            return query;
        }
    }
}
