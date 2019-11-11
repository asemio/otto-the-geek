# Authorizing Fields

Each field in an `OttoModel<T>` can be configured with authorization; that is, you can control whether or not the user is allowed to resolve that field based on your own authorization rules.

To authorize a field, use the `.Authorize(...)` method on `GraphTypeBuilder<T>`:


```csharp
public sealed class Model : OttoModel<Query>
{
    protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
    {
        return builder
            .GraphType<Thing>(thingBuilder =>
                thingBuilder
                    .Authorize(thing => thing.Name)
                        .Via<Authorizer>(x => x.CanViewName())
            );
    }
}
```

This assumes some class named `Authorizer` exists. `OttoTheGeek` will register this class with the dependency injection container on your behalf.

If authorization fails (i.e. the method passed to `.Via(...)` returns `false`), then the field's value will be null in the response, and an error with the message "Not authorized" will also be present. For instance, a query like:

```
{
    thing {
        name
    }
}
```
would return a response like this:
```
{
    "data" {
        "thing" {
            "name": null
        }
    },
    "errors": [
        {
            "message": "Not authorized",
            ...
        }
    ]
}
```