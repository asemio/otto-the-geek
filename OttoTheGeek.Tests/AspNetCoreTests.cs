using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

[assembly: WebApplicationFactoryContentRoot("OttoTheGeek.Tests", ".", "OttoTheGeek.Tests.dll", "0")]

namespace OttoTheGeek.Tests
{
    public sealed class AspNetCoreTests
    {
        public sealed class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                var model = new Model();

                services.AddOtto(model);
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseOtto<Model>(path: "/graphql");
            }
        }

        public sealed class WebAppFactory : WebApplicationFactory<Startup>
        {
            protected override IWebHostBuilder CreateWebHostBuilder()
            {
                var builder = new WebHostBuilder()
                    .UseStartup<Startup>()
                    .UseSetting("TEST_CONTENTROOT_OTTOTHEGEEK_TESTS", ".");

                return builder;
            }
        }

        public sealed class Query
        {
            public Child Child { get; set; }
            public Child ChildFromArgs { get; set; }
        }

        public sealed class Child
        {
            public int AnInt { get; set; }
            public string AString { get; set; }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .GraphType<Query>(b =>
                        b
                            .LooseScalarField(x => x.Child)
                                .ResolvesVia<Resolver>()
                            .LooseScalarField(x => x.ChildFromArgs)
                                .WithArgs<Child>()
                                .ResolvesVia<Resolver>()
                );
            }
        }

        public sealed class Resolver : ILooseScalarFieldResolver<Child>, ILooseScalarFieldWithArgsResolver<Child, Child>
        {
            public Task<Child> Resolve()
            {
                return Task.FromResult(new Child {
                    AnInt = 42,
                    AString = "Hello World!"
                });
            }

            public Task<Child> Resolve(Child args)
            {
                return Task.FromResult(args);
            }
        }

        [Fact]
        public async Task GraphQLMedia()
        {
            var webAppFactory = new WebAppFactory();

            var client = webAppFactory.CreateClient();

            var query = @"{
                child {
                    anInt
                    aString
                }
            }";

            var response = await client.PostAsync("/graphql", new StringContent(query, Encoding.ASCII, "application/graphql"));

            var rawResult = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(rawResult)["data"].ToString();

            result.Should().Be(JObject.Parse(@"{
                ""child"": {
                    ""anInt"": 42,
                    ""aString"": ""Hello World!""
                }
            }").ToString());
        }

        [Fact]
        public async Task JsonMedia()
        {
            var webAppFactory = new WebAppFactory();

            var client = webAppFactory.CreateClient();

            var query = @"query foo($myInt: Int!, $myStr: String!) {
                childFromArgs(anInt: $myInt, aString: $myStr) {
                    anInt
                    aString
                }
            }";

            var postdata = new JObject(
                new JProperty("query", query),
                new JProperty("variables", new JObject(
                    new JProperty("myInt", 12),
                    new JProperty("myStr", "Halloo variables")

                ))
            ).ToString();

            var response = await client.PostAsync("/graphql", new StringContent(postdata, Encoding.ASCII, "application/json"));

            var rawResult = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(rawResult)["data"].ToString();

            result.Should().Be(JObject.Parse(@"{
                ""childFromArgs"": {
                    ""anInt"": 12,
                    ""aString"": ""Halloo variables""
                }
            }").ToString(), $"full result: {rawResult}");
        }
    }
}
