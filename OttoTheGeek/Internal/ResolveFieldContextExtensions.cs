using System;
using GraphQL;

namespace OttoTheGeek.Internal
{
    public static class ResolveFieldContextExtensions
    {
        public static TArgs DeserializeArgs<TArgs>(this IResolveFieldContext context)
        {
            var args = (TArgs)Activator.CreateInstance(typeof(TArgs));

            foreach(var prop in typeof(TArgs).GetProperties())
            {
                if(!context.Arguments.TryGetValue(prop.Name.ToCamelCase(), out var propValue))
                {
                    continue;
                }

                // this GetPropertyValue is from GraphQL
                prop.SetValue(args, propValue.Value.GetPropertyValue(prop.PropertyType));
            }

            return args;
        }
    }
}
