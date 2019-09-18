using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
{
    public interface IGraphTypeBuilder
    {
        void ConfigureScalarQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache);
        void ConfigureListQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache);
    }
}
