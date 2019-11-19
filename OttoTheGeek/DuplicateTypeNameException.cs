using System;
using System.Collections.Generic;
using System.Linq;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public sealed class DuplicateTypeNameException : System.Exception
    {
        public DuplicateTypeNameException(string graphTypeName, IEnumerable<Type> types)
            : base(FormatErrorMessage(graphTypeName, types))
        {
        }

        private static string FormatErrorMessage(string graphTypeName, IEnumerable<Type> types)
        {
            var typeNames = string.Join($"{System.Environment.NewLine}    ", types.Select(x => x.FullName).OrderBy(x => x));
            return $@"The following C# types have the same GraphQL type name of ""{graphTypeName}"" configured:
    {typeNames}
Please configure unique type names for each.";
        }
    }
}