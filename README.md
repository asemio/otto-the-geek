# OttoTheGeek

OttoTheGeek is a library that helps you configure GraphQL types for use in your C# projects. It leverages [GraphQL .Net](https://graphql-dotnet.github.io/) under the hood and lets you use it exclusively, or mix-and-match with your existing GraphQL .Net code.

## Quick Start Guide

First, you'll need to define a class that inherits from `OttoModel<TQuery>`, where `TQuery` is the C# type that defines your root-level query object:

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
    protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
    {
        return builder.QueryField(x => x.Child)
            .ResolvesVia<Resolver>();
    }
}

public sealed class Resolver : IScalarFieldResolver<Child>
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

There are a number of different kinds of resolver interfaces that let you resolve nested fields, pass field arguments, etc.

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

## Resolver Interfaces

There are a handful of different resolver interfaces. The simplest one we saw above. It lets you resolve a scalar object anywhere. It's most useful on the query object, where there isn't a parent object that you're resolving from. There's another interface, `IListFieldResolver<TElem>` that works similarly, but for an `IEnumerable<TElem>`, mapping appropriately to the proper `LIST` type in GraphQL.

However, both `IScalarFieldResolver<TElem>` and `IListFieldResolver<TElem>` have a fundamental problem, and that is that they will suffer from the classic N+1 query problem. Consider this setup:

```cs
public sealed class Query
{
    public IEnumerable<Child> Children { get; set; }
}
public sealed class Child
{
    public int Id { get; set; }
    public IEnumerable<Grandchild> Children { get; set; }
}

public sealed class Grandchild
{
    public int AnInt { get; set; }
    public string AString { get; set; }
}
```

Ideally, we'd only want to go to our data store twice to get `Child` and `Grandchild` information via GraphQL; once for `Child` objects and once for `Grandchild` objects. For nested fields like this, there are the `IScalarFieldResolver<TContext, TModel>` and `IListFieldResolver<TContext, TElem>` interfaces that will automatically mitigate only resolving deeply-nested properties via a single call to load data rather than one call per "parent" object.
For our scenario above, this resolver will resolve `Grandchild` objects once, regardless of how many different `Child` objects they pertain to:

```cs
public sealed class GrandchildResolver : IListFieldResolver<ChildObject, GrandchildObject>
{
    public async Task<ILookup<object, GrandchildObject>> GetData(IEnumerable<object> keys)
    {
        await Task.CompletedTask;

        return keys
            .Cast<long>()
            .SelectMany(key => new[]{
                new GrandchildObject { AnInt = key * 1000 + 1, AString = "hi" },
                new GrandchildObject { AnInt = key * 1000 + 2, AString = "hi" }
            }, (key, child) => (key, child))
            .ToLookup(x => (object)x.Item1, x => x.Item2);
    }

    public object GetKey(Child context)
    {
        return context.Id;
    }
}
```
The model configuration for a setup like this would look something like:
```cs
public class Model : OttoModel<Query>
{
    protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
    {
        return builder
            .GraphType<ChildObject>(b =>
                b.ListField(x => x.Children)
                    .ResolvesVia<GrandchildResolver>()
            )
            .ListQueryField(x => x.Children)
            .ResolvesVia<ChildrenResolver>();
    }
}
```

## Field Arguments

TODO: explain field arguments

## Configuring Each Type

TODO: explain GraphTypeBuilder\<T>

## Dependency Injection and Integrating

TODO: show example of integrating this into an aspnet core app