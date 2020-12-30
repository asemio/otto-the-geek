using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace OttoTheGeek
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOtto<TModel>(this IApplicationBuilder app, string path = "/")
            where TModel : OttoModel
        {
            return app.UseGraphQL<ModelSchema<TModel>>(new PathString(path));
        }
    }
}