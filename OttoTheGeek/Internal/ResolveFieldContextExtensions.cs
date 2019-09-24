using System;
using GraphQL;
using GraphQL.Types;
using Newtonsoft.Json;

namespace OttoTheGeek.Internal
{
    public static class ResolveFieldContextExtensions
    {
        public static TArgs DeserializeArgs<TArgs>(this ResolveFieldContext context)
        {
            var args = (TArgs)Activator.CreateInstance(typeof(TArgs));

            foreach(var prop in typeof(TArgs).GetProperties())
            {
                if(!context.Arguments.TryGetValue(prop.Name.ToCamelCase(), out var propValue))
                {
                    continue;
                }

                prop.SetValue(args, propValue);
            }

            return args;
        }
    }
}