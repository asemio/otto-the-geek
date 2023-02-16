using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public sealed class OttoEnumGraphType<TEnum> : EnumerationGraphType
    {
        public OttoEnumGraphType()
        {
            Name = typeof(TEnum).Name;

            var valuesByName = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .ToDictionary(x => x.ToString());

            foreach(var member in typeof(TEnum).GetMembers().Where(x => valuesByName.ContainsKey(x.Name)))
            {
                var descAttr = member.GetCustomAttribute<DescriptionAttribute>();
                Values.Add(new EnumValueDefinition(member.Name, Enum.Parse(typeof(TEnum), member.Name))
                {
                    Description = descAttr?.Description,
                });
            }
        }
    }
}
