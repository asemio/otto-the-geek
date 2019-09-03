using System.Reflection;

namespace OttoTheGeek.Core
{
    public sealed class UnableToResolveException : System.Exception
    {
        public UnableToResolveException(PropertyInfo prop)
            : base($"Unable to resolve property {prop.Name} on class {prop.DeclaringType.Name}")
        {
        }
    }
}