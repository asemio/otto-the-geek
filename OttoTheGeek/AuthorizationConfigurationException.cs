using System.Reflection;

namespace OttoTheGeek
{
    public sealed class AuthorizationConfigurationException : System.Exception
    {
        public AuthorizationConfigurationException(PropertyInfo prop)
            : base($"Cannot configure authorization for non-nullable property {prop.Name} on class {prop.DeclaringType.Name}.")
        {
        }
    }
}