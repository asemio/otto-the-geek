using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public sealed class GraphTypeBuilder<TModel> : IGraphTypeBuilder
        where TModel : class
    {
        private static readonly IReadOnlyDictionary<Type, Type> CSharpToGraphqlTypeMapping = new Dictionary<Type, Type>{
            [typeof(string)]            = typeof(NonNullGraphType<StringGraphType>),
            [typeof(int)]               = typeof(NonNullGraphType<IntGraphType>),
            [typeof(long)]              = typeof(NonNullGraphType<IntGraphType>),
            [typeof(double)]            = typeof(NonNullGraphType<FloatGraphType>),
            [typeof(float)]             = typeof(NonNullGraphType<FloatGraphType>),
            [typeof(decimal)]           = typeof(NonNullGraphType<DecimalGraphType>),
            [typeof(bool)]              = typeof(NonNullGraphType<BooleanGraphType>),
            [typeof(DateTime)]          = typeof(NonNullGraphType<DateGraphType>),
            [typeof(DateTimeOffset)]    = typeof(NonNullGraphType<DateTimeOffsetGraphType>),
            [typeof(Guid)]              = typeof(NonNullGraphType<IdGraphType>),
            [typeof(short)]             = typeof(NonNullGraphType<ShortGraphType>),
            [typeof(ushort)]            = typeof(NonNullGraphType<UShortGraphType>),
            [typeof(ulong)]             = typeof(NonNullGraphType<ULongGraphType>),
            [typeof(uint)]              = typeof(NonNullGraphType<UIntGraphType>),

            [typeof(long?)]             = typeof(IntGraphType),
            [typeof(int?)]              = typeof(IntGraphType),
            [typeof(long?)]             = typeof(IntGraphType),
            [typeof(double?)]           = typeof(FloatGraphType),
            [typeof(float?)]            = typeof(FloatGraphType),
            [typeof(decimal?)]          = typeof(DecimalGraphType),
            [typeof(bool?)]             = typeof(BooleanGraphType),
            [typeof(DateTime?)]         = typeof(DateGraphType),
            [typeof(DateTimeOffset?)]   = typeof(DateTimeOffsetGraphType),
            [typeof(Guid?)]             = typeof(IdGraphType),
            [typeof(short?)]            = typeof(ShortGraphType),
            [typeof(ushort?)]           = typeof(UShortGraphType),
            [typeof(ulong?)]            = typeof(ULongGraphType),
            [typeof(uint?)]             = typeof(UIntGraphType),
            // TODO: timespan
        };
        private static readonly Dictionary<PropertyInfo, Type> NoResolvers = new Dictionary<PropertyInfo, Type>();
        private readonly Type _scalarQueryFieldResolver;
        private readonly Type _listQueryFieldResolver;
        private readonly Type _connectionResolver;
        private readonly Dictionary<PropertyInfo, Type> _scalarFieldResolvers;
        private readonly Dictionary<PropertyInfo, Type> _listFieldResolvers;
        private readonly IEnumerable<PropertyInfo> _propertiesToIgnore;

        public GraphTypeBuilder() : this(null, null, null, NoResolvers, NoResolvers, new PropertyInfo[0])
        {

        }
        private GraphTypeBuilder(
            Type scalarQueryFieldResolver,
            Type listQueryFieldResolver,
            Type connectionResolver,
            Dictionary<PropertyInfo, Type> scalarFieldResolvers,
            Dictionary<PropertyInfo, Type> listFieldResolvers,
            IEnumerable<PropertyInfo> propertiesToIgnore
            )
        {
            _scalarQueryFieldResolver = scalarQueryFieldResolver;
            _listQueryFieldResolver = listQueryFieldResolver;
            _connectionResolver = connectionResolver;
            _scalarFieldResolvers = scalarFieldResolvers;
            _listFieldResolvers = listFieldResolvers;
            _propertiesToIgnore = propertiesToIgnore;
        }

        public GraphTypeBuilder<TModel> WithScalarQueryFieldResolver<TResolver>()
            where TResolver : IQueryFieldResolver<TModel>
        {
            return Clone(scalarQueryFieldResolver: typeof(TResolver));
        }

        public GraphTypeBuilder<TModel> WithListQueryFieldResolver<TResolver>()
            where TResolver : IListQueryFieldResolver<TModel>
        {
            return Clone(listQueryFieldResolver: typeof(TResolver));
        }

        internal GraphTypeBuilder<TModel> WithConnectionResolver<TResolver>() where TResolver : IConnectionResolver<TModel>
        {
            return Clone(connectionResolver: typeof(TResolver));
        }

        public ScalarFieldBuilder<TModel, TProp> ScalarField<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new ScalarFieldBuilder<TModel, TProp>(this, prop);
        }

        public ListFieldBuilder<TModel, TProp> ListField<TProp>(Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new ListFieldBuilder<TModel, TProp>(this, prop);
        }

        public GraphTypeBuilder<TModel> IgnoreProperty<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();
            var props = _propertiesToIgnore.Concat(new[] { prop }).ToArray();

            return Clone(propertiesToIgnore: props);
        }

        internal GraphTypeBuilder<TModel> WithScalarFieldResolver<TProp, TResolver>(PropertyInfo prop)
            where TResolver : IScalarFieldResolver<TModel, TProp>
        {
            var dict = new Dictionary<PropertyInfo, Type>(_scalarFieldResolvers);
            dict[prop] = typeof(TResolver);

            return Clone(scalarFieldResolvers: dict);
        }

        internal GraphTypeBuilder<TModel> WithListFieldResolver<TProp, TResolver>(PropertyInfo prop)
            where TResolver : IListFieldResolver<TModel, TProp>
        {
            var dict = new Dictionary<PropertyInfo, Type>(_listFieldResolvers);
            dict[prop] = typeof(TResolver);

            return Clone(listFieldResolvers: dict);
        }

        void IGraphTypeBuilder.ConfigureScalarQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache)
        {
            if(_scalarQueryFieldResolver == null)
            {
                throw new UnableToResolveException(prop);
            }

            services.AddTransient(typeof(IQueryFieldResolver<TModel>), _scalarQueryFieldResolver);
            var myGraphType = BuildGraphType(services: services, cache: graphTypeCache);

            queryType.AddField(new FieldType {
                Name = prop.Name,
                ResolvedType = myGraphType,
                Type = prop.PropertyType,
                Resolver = new ScalarQueryFieldResolverProxy()
            });
        }

        void IGraphTypeBuilder.ConfigureListQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache)
        {
            services.AddTransient(typeof(IListQueryFieldResolver<TModel>), _listQueryFieldResolver);

            var myGraphType = BuildGraphType(services: services, cache: graphTypeCache);
            var listType = new ListGraphType(myGraphType);

            queryType.AddField(new FieldType {
                Name = prop.Name,
                ResolvedType = listType,
                Type = prop.PropertyType,
                Resolver = new ListQueryFieldResolverProxy()
            });
        }

        void IGraphTypeBuilder.ConfigureConnectionField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services, GraphTypeCache graphTypeCache)
        {
            services.AddTransient(typeof(IConnectionResolver<TModel>), _connectionResolver);

            var connectionType = graphTypeCache.Resolve<Connection<TModel>>(services);

            queryType.AddField(new FieldType {
                Name = prop.Name,
                ResolvedType = connectionType,
                Type = prop.PropertyType,
                Arguments = new QueryArguments(
                    new QueryArgument(typeof(NonNullGraphType<IntGraphType>)) { Name = nameof(PagingArgs.Count) },
                    new QueryArgument(typeof(NonNullGraphType<IntGraphType>)) { Name = nameof(PagingArgs.Offset) }
                ),
                Resolver = new ConnectionFieldResolverProxy()
            });
        }

        // TODO: take away the default args here
        public ObjectGraphType<TModel> BuildGraphType(GraphTypeCache cache = null, IServiceCollection services = null)
        {
            cache = cache ?? new GraphTypeCache();
            services = services ?? new ServiceCollection();
            var graphType = new ObjectGraphType<TModel>
            {
                Name = GraphTypeName
            };
            if(!cache.TryPrime(graphType))
            {
                return cache.Resolve<TModel>(services);
            }

            foreach(var prop in typeof(TModel).GetProperties().Except(_propertiesToIgnore))
            {
                if(CSharpToGraphqlTypeMapping.TryGetValue(prop.PropertyType, out var graphQlType))
                {
                    graphType.Field(
                        type: graphQlType,
                        name: prop.Name
                    );
                }
                else if(IsConnection && prop.Name == nameof(Connection<object>.Records)) {
                    var elemType = prop.PropertyType.GetEnumerableElementType();
                    var elemGraphType = cache.Resolve(elemType, services);
                    var listType = new ListGraphType(elemGraphType);
                    graphType.AddField(new FieldType {
                        Name = prop.Name,
                        Type = prop.PropertyType,
                        ResolvedType = listType
                    });
                }
                else if(_scalarFieldResolvers.TryGetValue(prop, out var resolverType))
                {
                    services.AddTransient(typeof(IScalarFieldResolver<,>).MakeGenericType(typeof(TModel), prop.PropertyType), resolverType);

                    graphType.AddField(new FieldType {
                        Name = prop.Name,
                        ResolvedType = cache.Resolve(prop.PropertyType, services),
                        Type = prop.PropertyType,
                        Resolver = (IFieldResolver)(Activator.CreateInstance(typeof(ScalarFieldResolverProxy<>).MakeGenericType(typeof(TModel), prop.PropertyType)))
                    });
                }
                else if(_listFieldResolvers.TryGetValue(prop, out var listResolverType))
                {
                    var elemType = prop.PropertyType.GetEnumerableElementType();
                    services.AddTransient(typeof(IListFieldResolver<,>).MakeGenericType(typeof(TModel), elemType), listResolverType);

                    graphType.AddField(new FieldType {
                        Name = prop.Name,
                        ResolvedType = new ListGraphType(cache.Resolve(elemType, services)),
                        Type = prop.PropertyType,
                        Resolver = (IFieldResolver)(Activator.CreateInstance(typeof(ListFieldResolverProxy<>).MakeGenericType(typeof(TModel), elemType)))
                    });
                }
                else
                {
                    throw new UnableToResolveException(prop);
                }
            }
            return graphType;
        }
        private abstract class ResolverProxyBase<T> : IFieldResolver<Task<T>>
        {
            public Task<T> Resolve(ResolveFieldContext context)
            {
                // this cast to Schema is gross...
                var resolver = ((Schema)context.Schema).DependencyResolver;

                return Resolve(context, resolver);
            }

            object IFieldResolver.Resolve(ResolveFieldContext context)
            {
                return Resolve(context);
            }
            protected abstract Task<T> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver);
        }
        private sealed class ScalarQueryFieldResolverProxy : ResolverProxyBase<TModel>
        {
            protected override Task<TModel> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver)
            {
                var resolver = dependencyResolver.Resolve<IQueryFieldResolver<TModel>>();

                return resolver.Resolve();
            }
        }
        private sealed class ListQueryFieldResolverProxy : ResolverProxyBase<IEnumerable<TModel>>
        {
            protected override Task<IEnumerable<TModel>> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver)
            {
                // this cast to Schema is gross...
                var resolver = dependencyResolver.Resolve<IListQueryFieldResolver<TModel>>();

                return resolver.Resolve();
            }
        }
        private sealed class ScalarFieldResolverProxy<TField> : ResolverProxyBase<TField>
        {
            protected override Task<TField> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver)
            {
                var loaderContext = dependencyResolver.Resolve<IDataLoaderContextAccessor>().Context;
                var resolver = dependencyResolver.Resolve<IScalarFieldResolver<TModel, TField>>();

                var loader = loaderContext.GetOrAddBatchLoader<object, TField>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys));

                return loader.LoadAsync(resolver.GetKey((TModel)context.Source));
            }
        }
        private sealed class ListFieldResolverProxy<TField> : ResolverProxyBase<IEnumerable<TField>>
        {
            protected override Task<IEnumerable<TField>> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver)
            {
                var loaderContext = dependencyResolver.Resolve<IDataLoaderContextAccessor>().Context;
                var resolver = dependencyResolver.Resolve<IListFieldResolver<TModel, TField>>();

                var loader = loaderContext.GetOrAddCollectionBatchLoader<object, TField>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys));

                return loader.LoadAsync(resolver.GetKey((TModel)context.Source));
            }
        }
        private sealed class ConnectionFieldResolverProxy : ResolverProxyBase<Connection<TModel>>
        {
            protected override Task<Connection<TModel>> Resolve(ResolveFieldContext context, IDependencyResolver dependencyResolver)
            {
                var resolver = dependencyResolver.Resolve<IConnectionResolver<TModel>>();
                var args = new PagingArgs {
                    Count = context.GetArgument<int>(nameof(PagingArgs.Count).ToCamelCase()),
                    Offset = context.GetArgument<int>(nameof(PagingArgs.Offset).ToCamelCase()),
                };
                return resolver.Resolve(args);
            }
        }
        private GraphTypeBuilder<TModel> Clone(
            Type scalarQueryFieldResolver = null,
            Type listQueryFieldResolver = null,
            Type connectionResolver = null,
            Dictionary<PropertyInfo, Type> scalarFieldResolvers = null,
            Dictionary<PropertyInfo, Type> listFieldResolvers = null,
            IEnumerable<PropertyInfo> propertiesToIgnore = null
            )
        {
            return new GraphTypeBuilder<TModel>(
                scalarQueryFieldResolver: scalarQueryFieldResolver ?? _scalarQueryFieldResolver,
                listQueryFieldResolver: listQueryFieldResolver ?? _listQueryFieldResolver,
                connectionResolver: connectionResolver ?? _connectionResolver,
                scalarFieldResolvers: scalarFieldResolvers ?? _scalarFieldResolvers,
                listFieldResolvers: listFieldResolvers ?? _listFieldResolvers,
                propertiesToIgnore: propertiesToIgnore ?? _propertiesToIgnore
            );
        }

        private string GraphTypeName =>
            IsConnection
            ? $"{GetConnectionElemType().Name}Connection"
            : typeof(TModel).Name;

        private bool IsConnection => typeof(TModel).IsConstructedGenericType
            && typeof(TModel).GetGenericTypeDefinition() == typeof(Connection<>);

        private Type GetConnectionElemType()
        {
            return typeof(TModel).GetGenericArguments().Single();
        }
    }
}
