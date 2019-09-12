using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Core
{

    public abstract class GraphTypeBuilder
    {
        // prevent funny business
        internal GraphTypeBuilder() {}
        public abstract void ConfigureScalarQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services);
        public abstract void ConfigureListQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services);
    }

    public sealed class GraphTypeBuilder<TModel> : GraphTypeBuilder
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

        private Type _scalarQueryFieldResolver;
        private Type _listQueryFieldResolver;

        public GraphTypeBuilder<TModel> WithScalarQueryFieldResolver<TResolver>()
            where TResolver : IQueryFieldResolver<TModel>
        {
            var newBuilder = new GraphTypeBuilder<TModel>();

            newBuilder._scalarQueryFieldResolver = typeof(TResolver);

            return newBuilder;
        }

        public GraphTypeBuilder<TModel> WithListQueryFieldResolver<TResolver>()
            where TResolver : IListQueryFieldResolver<TModel>
        {
            var newBuilder = new GraphTypeBuilder<TModel>();

            newBuilder._listQueryFieldResolver = typeof(TResolver);

            return newBuilder;
        }

        public override void ConfigureScalarQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services)
        {
            if(_scalarQueryFieldResolver == null)
            {
                throw new UnableToResolveException(prop);
            }

            services.AddTransient(typeof(IQueryFieldResolver<TModel>), _scalarQueryFieldResolver);

            queryType.AddField(new FieldType {
                Name = prop.Name,
                ResolvedType = BuildGraphType(),
                Type = prop.PropertyType,
                Resolver = new ScalarQueryFieldResolverProxy()
            });
        }

        public override void ConfigureListQueryField(PropertyInfo prop, ObjectGraphType queryType, IServiceCollection services)
        {
            services.AddTransient(typeof(IListQueryFieldResolver<TModel>), _listQueryFieldResolver);

            var myGraphType = BuildGraphType();
            var listType = new ListGraphType(myGraphType);

            queryType.AddField(new FieldType {
                Name = prop.Name,
                ResolvedType = listType,
                Type = prop.PropertyType,
                Resolver = new ListQueryFieldResolverProxy()
            });
        }

        public ObjectGraphType<TModel> BuildGraphType()
        {
            var graphType = new ObjectGraphType<TModel>
            {
                Name = typeof(TModel).Name
            };

            foreach(var prop in typeof(TModel).GetProperties())
            {
                if(CSharpToGraphqlTypeMapping.TryGetValue(prop.PropertyType, out var graphQlType))
                {
                    graphType.Field(
                        type: graphQlType,
                        name: prop.Name,
                        resolve: ctx => prop.GetValue(ctx.Source)
                    );
                }
                else
                {
                    throw new UnableToResolveException(prop);
                }
            }
            return graphType;
        }

        private sealed class ScalarQueryFieldResolverProxy : IFieldResolver<Task<TModel>>
        {
            public Task<TModel> Resolve(ResolveFieldContext context)
            {
                // this cast to Schema is gross...
                var resolver = ((Schema)context.Schema).DependencyResolver.Resolve<IQueryFieldResolver<TModel>>();

                return resolver.Resolve();
            }

            object IFieldResolver.Resolve(ResolveFieldContext context)
            {
                return Resolve(context);
            }
        }

        private sealed class ListQueryFieldResolverProxy : IFieldResolver<Task<IEnumerable<TModel>>>
        {
            public Task<IEnumerable<TModel>> Resolve(ResolveFieldContext context)
            {
                // this cast to Schema is gross...
                var resolver = ((Schema)context.Schema).DependencyResolver.Resolve<IListQueryFieldResolver<TModel>>();

                return resolver.Resolve();
            }

            object IFieldResolver.Resolve(ResolveFieldContext context)
            {
                return Resolve(context);
            }
        }
    }
}