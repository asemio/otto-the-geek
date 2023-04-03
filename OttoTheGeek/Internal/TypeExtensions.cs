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
            if(!t.IsEnumerable())
            {
                return null;
            }

            return t.GetGenericArguments().Single();
        }

        public static Type UnwrapGqlNetNonNullable(this Type t)
        {
            if(t.IsNonNullGraphType())
            {
                return t.GetGenericArguments().Single();
            }

            return t;
        }

        public static bool IsEnumerable(this Type t)
        {
            return t.IsGenericFor(typeof(IEnumerable<>));
        }

        public static bool IsGenericFor(this Type t, Type baseType)
        {
            while(t != null && t != typeof(object))
            {
                if(t.IsConstructedGenericType)
                {
                    if(t.GetGenericTypeDefinition() == baseType)
                    {
                        return true;
                    }
                }

                t = t.BaseType;
            }

            return false;
        }

        public static Type UnwrapNullable(this Type t)
        {
            if (t.IsNullable())
            {
                return t.GetGenericArguments().First();
            }

            return t;
        }

        public static Type UnwrapNullableAndEnumerable(this Type t)
        {
            return GetEnumerableElementType(t) ?? t.UnwrapNullable();
        }

        public static bool IsNullable(this Type t)
        {
            return t.IsGenericFor(typeof(Nullable<>));
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
