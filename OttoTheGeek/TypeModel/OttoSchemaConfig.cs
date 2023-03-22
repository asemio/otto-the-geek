using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.TypeModel;

public abstract record OttoSchemaConfig(
    ImmutableDictionary<Type, OttoTypeConfig> Types,
    OttoScalarTypeMap Scalars
    )
{
    public abstract Type QueryClrType { get; }
    public abstract Type MutationClrType { get; }

    public void RegisterResolvers(IServiceCollection services)
    {
        var resolverConfigs = Types.Values
            .SelectMany(x => x.Fields)
            .Select(x => x.Value.ResolverConfiguration)
            .Where(x => x != null);

        foreach (var r in resolverConfigs)
        {
            r.RegisterResolver(services);
        }
    }
}
public sealed record OttoSchemaConfig<TQuery, TMutation>(
    ImmutableDictionary<Type, OttoTypeConfig> Types,
    OttoScalarTypeMap Scalars
) : OttoSchemaConfig(Types, Scalars)
{
    public OttoSchemaConfig() : this(ImmutableDictionary<Type, OttoTypeConfig>.Empty, OttoScalarTypeMap.Default)
    {
    }

    public override Type QueryClrType => typeof(TQuery);
    public override Type MutationClrType => typeof(TMutation);
}
