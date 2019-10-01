# Configuring Each Type

For each C# type that's represented as some value that's returned (or as a field argument), that type has a corresponding GraphQL type. For simple scalars like `int`, `string`, `DateTime`, and so on, the GraphQL type is automatically generated for you. For complex object types (classes, interfaces) OttoTheGeek must inspect the type and build a GraphQL representation of that type. You can control elements of how that type is built via the `GraphType<T>(...)` method of `SchemaBuilder<TQuery>`. For situations where only a small tweak is needed, you can make these adjustments inline:

```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
    {
        return builder
            .GraphType<Thing>(thingBuilder => thingBuilder.Named("ThingType"));
    }
}
```

For more complex scenarios, consider factoring out a helper method:

```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
    {
        return builder
            .GraphType<Thing>(ConfigureThing);
    }

    private GraphTypeBuilder<Thing> ConfigureThing(GraphTypeBuilder<Thing> builder)
    {
        return builder.Named("ThingType")
            .ScalarField(x => x.Parent)
                .ResolvesVia<ThingParentResolver>()
            .Nullable(x => x.Price)
            ;
    }
}
```