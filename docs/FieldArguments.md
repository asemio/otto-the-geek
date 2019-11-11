# Field Arguments

Some GraphQL fields require arguments. For instance, if you have a query like this:
```graphql
{
    thing(id: 224) {
        name
        location
        price
    }
}
```
that `id` is an _argument_. OttoTheGeek models field arguments using an _args type_. An instance of your args type is passed to the resolver when resolving the field.

Representing the schema that would satisfy the above query might look something like this:

```csharp
public sealed class Query
{
    public Thing Thing { get; set; }
}

public sealed class Thing
{
    public string Name { get; set; }
    public string Location { get; set; }
    public decimal Price { get; set; }
}

public sealed class ThingArgs
{
    public int Id { get; set; }
}

public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder.QueryField(x => x.Thing)
            .WithArgs<ThingArgs>()
            .ResolvesVia<Resolver>();
    }
}

public sealed class Resolver : IScalarFieldWithArgsResolver<Child, ThingArgs>
{
    public Task<Child> Resolve(ThingArgs args)
    {
        return Task.FromResult(new Thing {
            Name = $"Thing {args.Id}",
            Location = "Stockroom",
            Price = 200m
        });
    }
}

```

*Note: The args type itself doesn't get represented in the GraphQL schema, but its fields do.*
