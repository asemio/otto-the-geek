using System;
using System.Linq;
using GraphQL;
using GraphQL.Types;

namespace OttoTheGeek
{
    public static class OrderValueGraphType
    {
        public static IGraphType FromOrderValueType(Type t)
        {
            var elemType = t.GetGenericArguments().Single();

            var graphTypeType = typeof(OrderValueGraphType<>).MakeGenericType(elemType);

            return (IGraphType)Activator.CreateInstance(graphTypeType);
        }
    }

    public sealed class OrderValueGraphType<T> : EnumerationGraphType
    {
        public OrderValueGraphType()
        {
            Name = $"{typeof(T).Name}OrderBy";

            foreach(var prop in typeof(T).GetProperties())
            {
                string propName = prop.Name.ToCamelCase();
                AddValue($"{propName}_ASC",  $"Order by {propName} ascending",  new OrderValue<T>(prop, false));
                AddValue($"{propName}_DESC", $"Order by {propName} descending", new OrderValue<T>(prop, true));
            }
        }
    }
}