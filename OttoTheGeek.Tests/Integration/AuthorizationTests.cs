using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OttoTheGeek.Tests.Integration
{
    public sealed class AuthorizationTests
    {
        public sealed class ScalarAuthModel : OttoModel<SimpleScalarQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleScalarQueryModel<ChildObject>>(b =>
                    b.LooseScalarField(x => x.Child)
                        .ResolvesVia<ChildResolver>()
                )
                .GraphType<ChildObject>(b =>
                    b
                        .Authorize(c => c.Protected)
                            .Via<Authorizer>(a => a.Authorize())
                        .Nullable(x => x.Protected)
                );
            }
        }

        public sealed class AsyncScalarAuthModel : OttoModel<SimpleScalarQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleScalarQueryModel<ChildObject>>(b =>
                    b.LooseScalarField(x => x.Child)
                        .ResolvesVia<ChildResolver>()
                )
                .GraphType<ChildObject>(b =>
                    b
                        .Authorize(c => c.Protected)
                            .Via<Authorizer>(a => a.AuthorizeAsync())
                        .Nullable(x => x.Protected)
                );
            }
        }

        public sealed class ObjectAuthModel : OttoModel<SimpleScalarQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleScalarQueryModel<ChildObject>>(b =>
                    b
                        .LooseScalarField(x => x.Child)
                            .ResolvesVia<ChildResolver>()
                        .Authorize(c => c.Child)
                            .Via<Authorizer>(a => a.Authorize())
                );
            }
        }

        public sealed class NullableMismatchAuthModel : OttoModel<SimpleScalarQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleScalarQueryModel<ChildObject>>(b =>
                    b.LooseScalarField(x => x.Child)
                        .ResolvesVia<ChildResolver>()
                )
                .GraphType<ChildObject>(b =>
                    b
                        .Authorize(c => c.Protected)
                            .Via<Authorizer>(a => a.Authorize())
                );
            }
        }

        public sealed class ChildResolver : ILooseScalarFieldResolver<ChildObject>
        {
            public Task<ChildObject> Resolve()
            {
                return Task.FromResult(new ChildObject());
            }
        }

        public sealed class ChildObject
        {
            public string Value1 => "hello";
            public string Protected => "world";
            public int Value3 => 654;
        }

        public class AuthBackend
        {
            public bool IsAuthorized { get; set; }
        }

        public class Authorizer
        {
            private readonly AuthBackend _backend;

            public Authorizer(AuthBackend backend)
            {
                _backend = backend;
            }
            public bool Authorize() => _backend.IsAuthorized;

            public Task<bool> AuthorizeAsync() => Task.FromResult(_backend.IsAuthorized);
        }

        [Fact]
        public async Task ScalarDenyAuthorization()
        {
            var backend = new AuthBackend
            {
                IsAuthorized = false
            };
            var server = new ScalarAuthModel()
                .CreateServer(services => services.AddSingleton(backend));

            var result = await server.GetResultAsync<JObject>(@"{
                child {
                    value1
                    protected
                    value3
                }
            }", "", throwOnError: false);

            var data = result["data"]["child"];
            data.Should().BeEquivalentTo(new JObject(
                new JProperty("value1", "hello"),
                new JProperty("protected", null),
                new JProperty("value3", 654)
            ));

            var errs = (JArray)result["errors"];
            errs.Count.Should().Be(1);
            errs[0]["message"].Value<string>().Should().Be("Not authorized");
        }

        [Fact]
        public async Task ScalarAllowAuthorization()
        {
            var backend = new AuthBackend
            {
                IsAuthorized = true
            };
            var server = new ScalarAuthModel()
                .CreateServer(services => services.AddSingleton(backend));

            var data = await server.GetResultAsync<JObject>(@"{
                child {
                    value1
                    protected
                    value3
                }
            }", "child");

            data.Should().BeEquivalentTo(new JObject(
                new JProperty("value1", "hello"),
                new JProperty("protected", "world"),
                new JProperty("value3", 654)
            ));
        }

        [Fact]
        public async Task ScalarAllowAuthorizationAsync()
        {
            var backend = new AuthBackend
            {
                IsAuthorized = true
            };
            var server = new AsyncScalarAuthModel()
                .CreateServer(services => services.AddSingleton(backend));

            var result = await server.GetResultAsync<JObject>(@"{
                child {
                    value1
                    protected
                    value3
                }
            }", "");

            var data = result["child"];
            data.Should().BeEquivalentTo(new JObject(
                new JProperty("value1", "hello"),
                new JProperty("protected", "world"),
                new JProperty("value3", 654)
            ));
        }

        [Fact]
        public async Task ObjectDenyAuthorization()
        {
            var backend = new AuthBackend
            {
                IsAuthorized = false
            };
            var server = new ObjectAuthModel()
                .CreateServer(services => services.AddSingleton(backend));

            var result = await server.GetResultAsync<JObject>(@"{
                child {
                    value1
                    protected
                    value3
                }
            }", "", throwOnError: false);

            result["data"]["child"].Should().BeEquivalentTo(new JObject());

            var errs = (JArray)result["errors"];
            errs.Count.Should().Be(1);
            errs[0]["message"].Value<string>().Should().Be("Not authorized");
        }

        [Fact]
        public async Task ObjectAllowAuthorization()
        {
            var backend = new AuthBackend
            {
                IsAuthorized = true
            };
            var server = new ObjectAuthModel()
                .CreateServer(services => services.AddSingleton(backend));

            var result = await server.GetResultAsync<JObject>(@"{
                child {
                    value1
                    protected
                    value3
                }
            }");

            result["child"].Should().BeEquivalentTo(new JObject(
                new JProperty("value1", "hello"),
                new JProperty("protected", "world"),
                new JProperty("value3", 654)
            ));
        }

        [Fact]
        public void NullableMismatchThrows()
        {
            new Action(() => new NullableMismatchAuthModel().CreateServer())
                .Should()
                .Throw<AuthorizationConfigurationException>()
                .And.Message.Should()
                .Be("Cannot configure authorization for non-nullable property Protected on class ChildObject.");
        }

    }
}
