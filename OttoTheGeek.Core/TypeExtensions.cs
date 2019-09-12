using System;
using System.Collections.Generic;
using System.Linq;

namespace OttoTheGeek.Core
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
    }
}