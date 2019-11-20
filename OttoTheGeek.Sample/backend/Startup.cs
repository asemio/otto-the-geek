using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var model = new Model();

            services
                .AddOtto(model)
                .AddTransient<ChildRepository>(); // the "backend" for our data
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseOtto<Model>(path: "/graphql");
        }
    }
}
