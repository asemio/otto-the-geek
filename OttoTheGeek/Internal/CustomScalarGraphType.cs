using System;
using GraphQL.NewtonsoftJson;
using GraphQLParser.AST;

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

        public override object ParseLiteral(GraphQLValue value)
        {
            if(value is GraphQLNullValue)
            {
                return null;
            }

            if(value is GraphQLStringValue strVal)
            {
                return _converter.Parse(new string(strVal.Value.Span));
            }

            if (value.Kind == ASTNodeKind.IntValue)
            {
                var innerVal = (GraphQLIntValue)value.GetValue();
                return _converter.Parse(new string(innerVal.Value.Span));
            }

            return _converter.Parse(value.GetValue().ToString());
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
            if(value is T castedValue)
            {
                return _converter.Convert(castedValue);
            }

            return null;
        }
    }
}
