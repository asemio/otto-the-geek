using System;
using System.Reflection;

namespace OttoTheGeek
{
    public sealed class UnableToResolveException : System.Exception
    {
        public UnableToResolveException(PropertyInfo prop, Type graphType)
            : base($"Unable to resolve property {prop.Name} on class {graphType.Name}")
        {
        }
    }
}