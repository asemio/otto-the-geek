using System;
using System.Linq.Expressions;
using System.Reflection;

namespace OttoTheGeek.Core
{
    public static class ExpressionExtensions
    {
        public static PropertyInfo PropertyInfoForSimpleGet<T, TProp>(this Expression<Func<T, TProp>> expr)
        {
            return (PropertyInfo)(((MemberExpression)expr.Body).Member);
        }
    }
}