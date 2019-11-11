# Configuring Each Type

For each C# type that's represented as some value that's returned (or as a field argument), that type has a corresponding GraphQL type. For simple scalars like `int`, `string`, `DateTime`, and so on, the GraphQL type is automatically generated for you. For complex object types (classes, interfaces) OttoTheGeek must inspect the type and build a GraphQL representation of that type. You can control elements of how that type is built via the `GraphType<T>(...)` method of `SchemaBuilder`. For situations where only a small tweak is needed, you can make these adjustments inline:

```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
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
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
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

See below for an explanation of the methods available on `GraphTypeBuilder<T>`.

## The `.Named(...)` method

Use `.Named("TypeName")` to override the name of the GraphQL type that Otto generates for your class. This is helpful if you have multiple classes, say, in different namespaces, that have the same name.


```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder
            .GraphType<Namespace1.Thing>(x => x.Named("Thing1"));
            .GraphType<Namespace2.Thing>(x => x.Named("Thing2"));
    }
}
```

## The `.Named(...)` method

Use `.Named("TypeName")` to override the name of the GraphQL type that Otto generates for your class. This is helpful if you have multiple classes, say, in different namespaces, that have the same name.


```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder
            .GraphType<Namespace1.Thing>(x => x.Named("Thing1"))
            .GraphType<Namespace2.Thing>(x => x.Named("Thing2"));
    }
}
```

## `.Nullable(...)` and `NotNullable(...)`

You can override the nullability that OttoTheGeek assigns to scalar fields with the `Nullable(...)` and `NotNullable(...)` methods. Pass a property expression to identify which property to make nullable or not nullable. This is helpful, for instance, if you want to allow nulls for string fields, which Otto treats as not nullable by default:

```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder
            .GraphType<Person>(x => x.Nullable(person => person.MiddleName));
    }
}
```

## `.ScalarField(...)`

The `.ScalarField(...)` lets you configure how a scalar field is resolved. Consider a model like this:
```csharp
public sealed class Product
{
    public int Id { get; set; }
    public Manufacturer Make { get; set; }
}
```

You must configure the `Make` property using the `.ScalarField(...)` method. There are a few different ways you can do this.

*Note: each example below assumes a `builder` variable of type `GraphTypeBuilder<Product>`*

To tell OttoTheGeek that the `Make` property will be loaded at the same time as its parent `Product`:

```csharp
builder.ScalarFi<eld(x => x.Make).Preloaded();
```

To tell OttoTheGeek that the `Make` property will be loaded by a resolver called `ProductMakeResolver`:

```csharp
builder.ScalarField(x => x.Make).ResolvesVia<ProductMakeResolver>();
// ProductMakeResolver must implement IScalarFieldResolver<Product, Manufacturer>
```


You can also use `.ScalarField(...)` to override the graph type of a scalar like `int`, `string`, etc. For instance, you could make GraphQL treat the `Id` property of the `Product` class as a non-nullable ID field like this:
```csharp
builder.ScalarField(x => x.Make).AsGraphType<NonNullGraphType<IdGraphType>>();
```
*Note: `NonNullGraphType<T>` and `IdGraphType` are defined by GraphQL .Net*

## `.LooseScalarField(...)`

A "loose" scalar field is one that is not "attached" to a parent; that is, it is resolved in isolation. **Be careful with loose fields because they will suffer from the N + 1 queries problem**:
```csharp
builder.LooseScalarField(x => x.Make).ResolvesVia<ProductMakeResolver>();
// ProductMakeResolver must implement IScalarFieldResolver<Manufacturer>
```

## `.ListField(...)`

A list field must be an `IEnumerable<T>`. List fields can be loaded via a resolver or preloaded. Consider a model like this:

```csharp
public sealed class Product
{
    public int Id { get; set; }
    public IEnumerable<Part> Parts { get; set; }
}
```

Preloaded:
```csharp
builder.ListField(x => x.Parts).Preloaded();
```
Via resolver:
```csharp
builder.ListField(x => x.Parts).ResolvesVia<ProductPartResolver>();
// ProductPartResolver must implement IListFieldResolver<Product, Part>
```
## `.LooseListField(...)`

Loose list fields work similarly to loose scalar fields, but must be `IEnumerable<T>` and will be treated as `LIST` types in GraphQL. Again, be careful, because these will suffer from the N+1 queries problem.

## `.Interface<TInterface>()`

Configures the graph type to implement the [GraphQL interface](https://graphql.org/learn/schema/#interfaces) represented by `TInterface`.

## `.ConfigureOrderBy<TEntity>(...)`

See the documentation for [paging](Paging).