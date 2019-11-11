# Configuring Mutations

Many of the samples in the documentation refer to just a `Query` type. You can define both query and mutation types by deriving your model from `OttoModel<TQuery, TMutation>` where `TQuery` is your query type and `TMutation` is your mutation type. All other configuration is identical for both query and mutation types.

Mutations, in compliance with the GraphQL spec, will always be executed before queries.

