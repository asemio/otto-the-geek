---
layout: default
---

# Model Validation

`OttoTheGeek` will examine your model types and build GraphQL
types for each of them. For classes, Otto will build `OBJECT`
types where the public properties of your classes will become
fields on the GraphQL type. For scalar values, Otto will map
to appropriate GraphQL types, such as `INT`, `BOOL`, etc.
according to the [table](#c-to-graphql-net-type-mapping) at the bottom of this file.

For values that aren't seen as scalars, `OttoTheGeek` requires you to configure how these fields are resolved. If any field in any type in your model hasn't been configured, building a schema from your `OttoModel<TQuery>`-derived model class will throw an `UnableToResolveException` that will give you the information about which class and which property was left unconfigured.

## C# to GraphQL .Net Type Mapping

| C# Type | GraphQL .Net Type |
|-|-|
| string | NonNullGraphType\<StringGraphType> |
| int | NonNullGraphType\<IntGraphType> |
| long | NonNullGraphType\<IntGraphType> |
| double | NonNullGraphType\<FloatGraphType> |
| float | NonNullGraphType\<FloatGraphType> |
| decimal | NonNullGraphType\<DecimalGraphType> |
| bool | NonNullGraphType\<BooleanGraphType> |
| DateTime | NonNullGraphType\<DateGraphType> |
| DateTimeOffset | NonNullGraphType\<DateTimeOffsetGraphType> |
| Guid | NonNullGraphType\<IdGraphType> |
| short | NonNullGraphType\<ShortGraphType> |
| ushort | NonNullGraphType\<UShortGraphType> |
| ulong | NonNullGraphType\<ULongGraphType> |
| uint | NonNullGraphType\<UIntGraphType> |
| TimeSpan | NonNullGraphType\<TimeSpanGraphType> |
| long | IntGraphType |
| int | IntGraphType |
| long | IntGraphType |
| double | FloatGraphType |
| float | FloatGraphType |
| decimal | DecimalGraphType |
| bool | BooleanGraphType |
| DateTime | DateGraphType |
| DateTimeOffset | DateTimeOffsetGraphType |
| Guid | IdGraphType |
| short | ShortGraphType |
| ushort | UShortGraphType |
| ulong | ULongGraphType |
| uint | UIntGraphType |
| TimeSpan | TimeSpanGraphType |

However, for fields th