using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public static class TypeExtensions
    {
        public static Type GetEnumerableElementType(this Type t)
        {
            if(!t.IsConstructedGenericType)
            {
                return null;
            }

            var genericType = t.GetGenericTypeDefinition();

            if(genericType != typeof(IEnumerable<>))
            {
                return null;
            }

            return t.GetGenericArguments().Single();
        }

        public static Type UnwrapNonNullable(this Type t)
        {
            if(t.IsNonNullGraphType())
            {
                return t.GetGenericArguments().Single();
            }

            return t;
        }

        public static Type MakeNonNullable(this Type t)
        {
            if(t.IsNonNullGraphType())
            {
                return t;
            }

            return typeof(NonNullGraphType<>).MakeGenericType(t);
        }

        private static bool IsNonNullGraphType(this Type t)
        {
            return (t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(NonNullGraphType<>));
        }
    }
}