using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OttoTheGeek
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<TElement> OrderBy<TElement>(this IQueryable<TElement> source,
            OrderValue<TElement> order)
        {
            if (order.Prop == null)
            {
                var elementName = typeof(TElement).Name;
                throw new InvalidOperationException(
                    $"Cannot order IQueryable<{elementName}> by \"{order.Name}\"; use a built-in property name on {elementName} or apply your own custom ordering logic rather than using OrderBy(...) with an OrderValue<{elementName}>");
            }

            var elementType = typeof(TElement);
            var param = Expression.Parameter(elementType);
            var memberAccess = Expression.MakeMemberAccess(param, order.Prop);

            var lambda = Expression.Lambda(memberAccess, param);

            var method = typeof(QueryableExtensions).GetMethod(nameof(OrderByExpression), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(typeof(TElement), order.Prop.PropertyType);

            return (IOrderedQueryable<TElement>)genericMethod.Invoke(null, new object[] { source, lambda, order.Descending });
        }

        private static IQueryable<T> OrderByExpression<T, TKey>(IQueryable<T> source, Expression<Func<T, TKey>> expression, bool descending)
        {
            if (descending)
            {
                return source.OrderByDescending(expression);
            }
            return source.OrderBy(expression);
        }
    }
}
