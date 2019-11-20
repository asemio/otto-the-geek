using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Server;

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
            ConfigureGraphQLNet(services);

            var schema = new Model().BuildGraphQLSchema(services);

            services.AddSingleton<ModelSchema>(schema);
        }

        // This method handles the configuration for the GraphQL .Net server
        private static void ConfigureGraphQLNet(IServiceCollection services)
        {
            services
                .AddGraphQL()
                .AddGraphTypes(ServiceLifetime.Scoped)
                .AddDataLoader();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseGraphQL<ModelSchema>(path: "/graphql");
        }
    }

    public sealed class Model : OttoModel<Query>
    {
        protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
        {
            return builder.GraphType<Query>(b =>
                b.LooseScalarField(x => x.Child)
                    .ResolvesVia<ChildResolver>()
                    );
        }

    }

    public sealed class Child
    {
        public string Hello => "Hello world!";
    }

    public sealed class Query
    {
        public Child Child { get; set; }
    }

    public sealed class ChildResolver : IScalarFieldResolver<Child>
    {
        public Task<Child> Resolve()
        {
            return Task.FromResult(new Child());
        }
    }
}
