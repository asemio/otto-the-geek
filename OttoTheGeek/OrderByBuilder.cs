using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL;
using GraphQL.Types;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public abstract class OrderByBuilder
    {
        internal OrderByBuilder() { /* no funny business inheriting outside the assembly */ }

        public abstract IGraphType BuildGraphType();

        public static OrderByBuilder FromPropertyInfo(PropertyInfo prop)
        {
            // prop is assumed to be OrderValue<TEntity>
            var entityType = prop.PropertyType.GetGenericArguments().Single();

            return (OrderByBuilder)Activator.CreateInstance(typeof(OrderByBuilder<>).MakeGenericType(entityType));
        }

    }
    public sealed class OrderByBuilder<T> : OrderByBuilder
    {
        private readonly IEnumerable<PropertyInfo> _propsToIgnore;
        private readonly Dictionary<string, AscDesc> _customValues;
        [Flags]
        private enum AscDesc
        {
            None = 0,
            Asc = 1,
            Desc = 2,
            Both = 3
        }

        public OrderByBuilder() : this(new PropertyInfo[0], new Dictionary<string, AscDesc>())
        {
        }
        private OrderByBuilder(
            IEnumerable<PropertyInfo> propsToIgnore,
            Dictionary<string, AscDesc> customValues
            )
        {
            _propsToIgnore = propsToIgnore;
            _customValues = customValues;
        }
        public OrderByBuilder<T> Ignore<TProp>(Expression<Func<T, TProp>> propExpr)
        {
            var prop = propExpr.PropertyInfoForSimpleGet();

            return new OrderByBuilder<T>(_propsToIgnore.Concat(new[] { prop}).ToArray(), _customValues);
        }

        public OrderByBuilder<T> AddValue(string name, bool descending)
        {
            var dict = new Dictionary<string, AscDesc>(_customValues);
            dict.TryGetValue(name, out var ascDesc);
            ascDesc |= (descending ? AscDesc.Desc : AscDesc.Asc);
            dict[name] = ascDesc;
            return new OrderByBuilder<T>(_propsToIgnore, dict);
        }

        public override IGraphType BuildGraphType()
        {
            var graphType = new EnumerationGraphType();

            graphType.Name = $"{typeof(T).Name}OrderBy";

            foreach(var prop in typeof(T).GetProperties().Except(_propsToIgnore))
            {
                string propName = prop.Name.ToCamelCase();
                graphType.Values.Add(new EnumValueDefinition($"{propName}_ASC", new OrderValue<T>(prop, descending: false)) {
                    Description = $"Order by {propName} ascending",
                });
                graphType.Values.Add(new EnumValueDefinition($"{propName}_DESC", new OrderValue<T>(prop, descending: true)) {
                    Description = $"Order by {propName} descending",
                });
            }

            foreach(var name in _customValues.Keys)
            {
                var sortOrder = _customValues[name];
                if(sortOrder.HasFlag(AscDesc.Asc))
                {
                    graphType.Values.Add(new EnumValueDefinition($"{name}_ASC", new OrderValue<T>(name, descending: false)) {
                        Description = $"Order by {name} ascending",
                    });
                }
                if(sortOrder.HasFlag(AscDesc.Desc))
                {
                    graphType.Values.Add(new EnumValueDefinition($"{name}_DESC", new OrderValue<T>(name, descending: true)) {
                        Description = $"Order by {name} descending",
                    });
                }
            }

            return graphType;
        }

    }
}
