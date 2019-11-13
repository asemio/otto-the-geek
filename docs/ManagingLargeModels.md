# Managing Large Models

Your `OttoModel<T>`-derived classes may become unwieldy and complex over time as your GraphQL model reaches a certain level of maturity. When this happens, you can spread the model configuration across a number of different classes using `IGraphTypeConfigurator<TModel, TType>` in combination with the `LoadConfigurators(...)` method on `OttoModel<T>`:

```csharp
    public sealed class Model : OttoModel<Query>
    {
        protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
        {
            return LoadConfigurators(this.GetType().Assembly, builder);
        }
    }

    public sealed class Configurator : IGraphTypeConfigurator<Model, Query>
    {
        public GraphTypeBuilder<Query> Configure(GraphTypeBuilder<Query> builder)
        {
            return builder
                .LooseScalarField(x => x.Child)
                    .ResolvesVia<Resolver>()
                    ;
        }

    }
```

You are free to implement as many different `IGraphTypeConfigurator<TModel,TType>` interfaces as you want on a single class; for instance:

```csharp
    public sealed class Configurator
        : IGraphTypeConfigurator<Model, Type1>
        , IGraphTypeConfigurator<Model, Type2>
        , IGraphTypeConfigurator<Model, Type3>
    {

        public GraphTypeBuilder<Type1> Configure(GraphTypeBuilder<Type1> builder)
        { /* implementation */ }

        public GraphTypeBuilder<Type2> Configure(GraphTypeBuilder<Type2> builder)
        { /* implementation */ }

        public GraphTypeBuilder<Type3> Configure(GraphTypeBuilder<Type3> builder)
        { /* implementation */ }
    }
```

The only caveat is that the first type argument must be **the exact same type** as your `OttoModel<T>`-derived model class - inheritance does not apply here. In other words, a class implementing `IGraphTypeConfigurator<OttoModel<Query>, Query>` will not be loaded by `Model`.