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
        internal OrderValue(PropertyInfo prop, bool descending)
        {
            Prop = prop;
            Name = prop.Name;
            Descending = descending;
        }

        internal OrderValue(string name, bool descending)
        {
            Name = name;
            Descending = descending;
        }

        public string Name { get; }
        public PropertyInfo Prop { get; }
        public bool Descending { get; }

    }
}