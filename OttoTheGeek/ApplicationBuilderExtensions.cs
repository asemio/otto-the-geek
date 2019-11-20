using GraphQL.Server;
using Microsoft.AspNetCore.Builder;

namespace OttoTheGeek
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOtto<TModel>(this IApplicationBuilder app, string path = "/")
            where TModel : OttoModel
        {
            return app.UseGraphQL<ModelSchema<TModel>>(path: path);
        }
    }
}