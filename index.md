---
layout: default
---

OttoTheGeek is a library that helps you configure GraphQL types for use in your C# projects. It leverages [GraphQL .Net](https://graphql-dotnet.github.io/) under the hood and lets you use it exclusively, or mix-and-match with your existing GraphQL .Net code.

## Quick Start Guide

First, you'll need to install `OttoTheGeek` into your project:

```
> dotnet add package OttoTheGeek
```

Next, you'll need to define a class that inherits from `OttoModel<TQuery>`, where `TQuery` is the C# type that defines your root-level query object:

```csharp
public sealed class Query
{
    public Child Child { get; set; }
}

public sealed class Child
{
    public int AnInt { get; set; }
    public string AString { get; set; }
}

public sealed class Model : OttoModel<Query> {}
```
In this case, the `Query` type here only has a single scalar property of type `Child` called `Child`.

Next, you'll need to tell your model how to _resolve_ fields on your query type, which is what allows `OttoTheGeek` to actually return data for your GraphQL queries. In order to resolve a field, you define a resolver and register it for your field in your model:

```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder
            .GraphType<Query>(b =>
                b.LooseScalarField(x => x.Child)
                    .ResolvesVia<Resolver>()
        );
    }
}

public sealed class Resolver : ILooseScalarFieldResolver<Child>
{
    public Task<Child> Resolve()
    {
        return Task.FromResult(new Child {
            AnInt = 42,
            AString = "Hello World!"
        });
    }
}

```

There are a number of different kinds of [resolver interfaces](docs/ResolverInterfaces) that let you resolve nested fields, pass field arguments, etc.

*NOTE: You shouldn't need to define resolvers for your typical scalar value types (`int`, `string`, and so on).*

Next, let's run a query! here we're using `OttoServer` which is a built-in synchronous GraphQL server that's handy for writing automated tests and experimenting.

```cs
var server = new Model().CreateServer();

var result = server.Execute<string>(@"{
    child {
        anInt
        aString
    }
}");

result.Should().Be(JObject.Parse(@"{
    ""child"": {
        ""anInt"": 42,
        ""aString"": ""Hello World!""
    }
}").ToString());
```

For further reading:

* [Resolver Interfaces](docs/ResolverInterfaces.md)
* [Model Validation](docs/ModelValidation.md)
* [Field Arguments](docs/FieldArguments.md)
* [Configuring Each Type](docs/ConfiguringEachType.md)
* [Paging and Sorting](docs/Paging.md)
* [Authorization](docs/Authorization.md)
* [Mutations](docs/Mutations.md)
* [Managing Large Models](docs/ManagingLargeModels.md)
* [ASP.Net Core Sample](https://github.com/asemio/otto-the-geek/tree/master/OttoTheGeek.Sample)
