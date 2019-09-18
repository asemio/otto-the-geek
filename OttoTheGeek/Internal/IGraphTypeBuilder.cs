using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public interface IGraphTypeBuilder
    {
        void ConfigureScalarQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache);
        void ConfigureListQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache);
        void ConfigureConnectionField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache);
    }
}
