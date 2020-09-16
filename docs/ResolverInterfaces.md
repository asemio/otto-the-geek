---
layout: default
---

# Resolver Interfaces

In order to return data from your GraphQL endpoint, you have to configure `OttoTheGeek` so that it understands how to _resolve_ fields on your types. In order to resolve a field, you define a resolver and register it for your field in your model:

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

public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder.QueryField(x => x.Child)
            .ResolvesVia<Resolver>();
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

The example above uses `ILooseScalarFieldResolver<TModel>`: it lets you resolve a scalar object anywhere. It's most useful on the query object, where there isn't a parent object that you're resolving from. There's another interface, `ILooseListFieldResolver<TElem>` that works similarly, but is intended to map an `IEnumerable<TElem>` in the model to an appropriate `LIST` type in GraphQL.

# The N + 1 Problem

Both `ILooseScalarFieldResolver<TElem>` and `ILooseListFieldResolver<TElem>` have a fundamental problem, and that is that they will suffer from the classic N+1 query problem. Consider this setup:

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
    public int ParentId { get; set; }
    public int AnInt { get; set; }
    public string AString { get; set; }
}
```

It would be really unfortunate if, for 100 `Child` objects and 1000 `Grandchild` objects, it required issuing 101 queries; one for the 100 `Child` objects, and one query each to fetch the `Granchild` objects for each `Child`.
Ideally, we'd only want to go to our data store twice to get `Child` and `Grandchild` information via GraphQL; once for `Child` objects and once for `Grandchild` objects.
For nested fields like this, there are the `IScalarFieldResolver<TContext, TModel>` and `IListFieldResolver<TContext, TElem>` interfaces that will automatically mitigate only resolving deeply-nested properties via a single call to load data rather than one call per "parent" object.
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
                new GrandchildObject {
                    ParentId = key,
                    AnInt = key * 1000 + 1,
                    AString = "hi"
                },
                new GrandchildObject {
                    ParentId = key,
                    AnInt = key * 1000 + 2,
                    AString = "hi"
                },
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
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder
            .ListQueryField(x => x.Children)
                .ResolvesVia<ChildrenResolver>()
            .GraphType<ChildObject>(b =>
                b.ListField(x => x.Children)
                    .ResolvesVia<GrandchildResolver>()
            )
            ;
    }
}
```