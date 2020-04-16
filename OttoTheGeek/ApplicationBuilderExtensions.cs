using GraphQL.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace OttoTheGeek
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOtto<TModel>(this IApplicationBuilder app, string path = "/")
            where TModel : OttoModel
        {
            return app.UseMiddleware<GraphQLNetHacks.Middleware<ModelSchema<TModel>>>(new PathString(path));
        }
    }
}