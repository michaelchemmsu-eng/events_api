using System.Linq.Expressions;
namespace project.Services.EventSpecificator
{
    /// <summary>
    /// абстрактный тип спецификации. Все кастомные спецификации наследуются от этого типа
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Specification<T>
    {
        /// <summary>
        /// выражение для фильтрации по критерию where
        /// </summary>
        public Expression<Func<T, bool>>? Criteria { get; protected init; }
        /// <summary>
        /// подгрузка связанных данных include через выражения
        /// </summary>
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        /// <summary>
        /// подгрузка связанных данных include через строки
        /// </summary>
        public List<string> IncludeStrings { get; } = new();// Подгрузка связанных данных include через строки
        /// <summary>
        /// Условие сортировки по возрастанию
        /// </summary>
        public Expression<Func<T, object>>? OrderBy { get; protected init; }
        /// <summary>
        /// условие сортировки по убыванию
        /// </summary>
        public Expression<Func<T, object>>? OrderByDescending { get; protected init; }
        /// <summary>
        /// Последующая сортировка
        /// </summary>
        public Expression<Func<T, object>>? ThenBy { get; protected init; }
        /// <summary>
        /// Номер страницы в результате
        /// </summary>
        public int? Take { get; private set; }
        /// <summary>
        /// число пропущенных записей
        /// </summary>

        public int? Skip { get; private set; }
        
        /// <summary>
        /// флаг , указывающий, что результат пагинирован
        /// </summary>
        public bool IsPagingEnabled { get; private set; }
        /// <summary>
        /// метод, который включает выражение для подгрузки связанных данных
        /// </summary>
        /// <param name="include"></param>
        protected void AddInclude(Expression<Func<T, object>> include)
        {
            Includes.Add(include);
        }

        /// <summary>
        /// метод, который включает выражение в виде строки для подгрузки связанных данных
        /// </summary>
        /// <param name="includeString"></param>
        protected void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }
        /// <summary>
        /// Метод, который применяет критерии пагинации
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        protected void ApplyPaging(int skip, int take)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }


    }
}
