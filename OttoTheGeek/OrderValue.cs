using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OttoTheGeek
{
    public abstract class OrderValue
    {
        internal OrderValue()
        {
            // prevent outside inheritance
        }
    }

    public sealed class OrderValue<T> : OrderValue
    {
        public OrderValue(PropertyInfo prop, bool descending)
        {
            Prop = prop;
            Descending = descending;
        }

        public PropertyInfo Prop { get; }
        public bool Descending { get; }

        public IEnumerable<T> ApplyOrdering(IEnumerable<T> items)
        {
            if(Descending)
            {
                return items.OrderByDescending(x => Prop.GetValue(x));
            }

            return items.OrderBy(x => Prop.GetValue(x));
        }
    }
}