# Configuring Paging and Ordering

Consider a model that looks like this:

```csharp
public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ModelNumber { get; set; }
    public Manufacturer Manufacturer { get; set; }
    public DateTime FirstRunDate { get; set; }
}
public sealed class Query
{
    public IEnumerable<Product> Products { get; set; }
}
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        builder.ConnectionField(x => x.Products)
            .ResolvesVia<ProductConnectionResolver>()
            // other things here as well possibly
            ;

    }
}
```
By configuring `Products` as a connection field, OttoTheGeek will set up `Products` as a field that takes arguments specified by the `PagingArgs<Product>` class (a built-in OttoTheGeek class). This enables your code to return pages of results rather than all the results at once. Resolvers for connection fields return a `Connection<T>` object and take a `PagingArgs<T>`. For example:

```csharp
public sealed class ProductConnectionResolver : IConnectionResolver<Product>
{

    public async Task<Connection<Product>> Resolve(PagingArgs<Product> args)
    {
        // use args.Offset, args.Count, and args.OrderBy to return a page of results
    }
}
```

One of the properties of `PagingArgs<Product>` is `OrderBy`, which is of type `OrderValue<Product>`. OttoTheGeek configures this as an `ENUM` type in GraphQL, and uses the properties of the model to determine its values. By default, the enum values of `OrderValue<Product>` for the model above would be:

* `Id_ASC`
* `Id_DESC`
* `Name_ASC`
* `Name_DESC`
* `ModelNumber_ASC`
* `ModelNumber_DESC`
* `FirstRunDate_ASC`
* `FirstRunDate_DESC`

If you wanted, say, the first 20 products ordered by ModelNumber, a query might look like:
```graphql
{
    products(orderBy: ModelNumber_ASC, count: 20, offset: 0) {
        id
        name
        modelNumber
    }
}
```

When OttoTheGeek calls the `Resolve(...)` method of `ProductConnectionResolver`, the value of the `OrderBy` field will be an `OrderValue<Product>`. The properties are:

* Name <br />
  The name of the property being sorted by
* Prop <br />
  The `System.Reflection.PropertyInfo` corresponding to the property being sorted by. This may be null if this is a custom sort value (see below)
* Descending <br />
  Indicates that the returned results should be sorted descending

By default, `Prop` will always be non-null. However, you can customize what sort values are available.
Let's say that we don't want to sort by `Id`.
Let's also say that we want to order by manufacturer name. However, the end-user may or may not select that field in their GraphQL query, so we can't assume that data will be present - we just want to make it possible to order by it:


```csharp
protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
{
    builder.ConnectionField(x => x.Products)
        .ResolvesVia<ProductConnectionResolver>()
        .GraphType<PagingArgs<Product>>(ConfigureProductPagingArgs)
        // other things here as well
        ;

}

private GraphTypeBuilder<PagingArgs<Product>> ConfigureProductPagingArgs(GraphTypeBuilder<PagingArgs<Product>> builder)
{
    return builder.ConfigureOrderBy(
            x => x.OrderBy,
            ConfigureProductOrderBy
            );
}

private OrderByBuilder<Product> ConfigureProductOrderBy(OrderByBuilder<Product> builder)
{
    return builder
        .Ignore(x => x.Id);
        .AddValue("ManufacturerName", descending: false)
        .AddValue("ManufacturerName", descending: true);
}
```

This will yield the following enum values:

* `Name_ASC`
* `Name_DESC`
* `ModelNumber_ASC`
* `ModelNumber_DESC`
* `FirstRunDate_ASC`
* `FirstRunDate_DESC`
* `ManufacturerName_ASC`
* `ManufacturerName_DESC`

If the user chooses to sort using the manufacturer name, the `Prop` value of the `PagingArgs<Product>` will be null, since it doesn't correspond to a property of the `Product` class:

```csharp
public sealed class ProductConnectionResolver : IConnectionResolver<Product>
{

    public async Task<Connection<Product>> Resolve(PagingArgs<Product> args)
    {
        if(args.OrderBy.Name == "ManufacturerName")
        {
            // sort differently since this is a custom sort value
        }
        else
        {
            // sort based on args.OrderBy.Prop
        }
        // use args.Offset, args.Count to select page
    }
}
```

## Custom `PagingArgs<T>`

In some cases, you may need to pass additional arguments for a connection field, such as filtering criteria. In this case, you can define a custom `PagingArgs<T>` class that contains your additional arguments. For example, let's say we want to add a search text argument to our `Products` field. We define our subclass:

```csharp
public sealed class ProductArgs : PagingArgs<Product>
{
    public string SearchText { get; set; }
}
```

Then register it when we define our connection field:

```csharp
builder.ConnectionField(x => x.Products)
    .WithArgs<ProductArgs>()
    .ResolvesVia<ProductConnectionResolver>()
```
And update our resolver to implement `IConnectionResolver<Product, ProductArgs>`:

```csharp
public sealed class ProductConnectionResolver : IConnectionResolver<Product, ProductArgs>
{

    public async Task<Connection<Product>> Resolve(ProductArgs args)
    {
        // implementation here; use args.SearchText as needed
    }
```