using GraphQL.Language.AST;

namespace OttoTheGeek.Internal
{
    public sealed class CustomScalarGraphType<T, TConverter> : GraphQL.Types.ScalarGraphType
        where TConverter : ScalarTypeConverter<T>, new()
    {
        private static readonly TConverter _converter = new TConverter();

        public CustomScalarGraphType()
        {
            Name = typeof(T).Name;
        }

        public override object ParseLiteral(IValue value)
        {
            if(value is NullValue)
            {
                return null;
            }

            return _converter.Parse(value.Value.ToString());
        }

        public override object ParseValue(object value)
        {
            if(value is string str)
            {
                return _converter.Parse(str);
            }

            return null;
        }

        public override object Serialize(object value)
        {
            return _converter.Convert((T)value);
        }
    }
}