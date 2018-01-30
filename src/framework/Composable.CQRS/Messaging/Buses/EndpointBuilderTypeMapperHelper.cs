using System.Collections.Generic;
using Composable.Refactoring.Naming;

namespace Composable.Messaging.Buses
{
    static class EndpointBuilderTypeMapperHelper
    {
        static string WithPostFix(this string guidTemplate, char postfix) => guidTemplate.Substring(0, guidTemplate.Length - 1) + postfix;

        static class Postfix
        {
            internal const char TypeItself = '1';
            internal const char Array = '2';
            internal const char List = '3';
            internal const char StringDictionary = '4';
        }

        public static ITypeMappingRegistar MapTypeAndStandardCollectionTypes<TType>(this ITypeMappingRegistar @this, string guidTemplate)
        {
            @this.Map<TType>(guidTemplate.WithPostFix(Postfix.TypeItself));

            @this.MapStandardCollectionTypes<TType>(guidTemplate);
            return @this;
        }

        public static ITypeMappingRegistar MapStandardCollectionTypes<TType>(this ITypeMappingRegistar @this, string guidTemplate)
        {
            @this.Map<TType[]>(guidTemplate.WithPostFix(Postfix.Array));
            @this.Map<List<TType>>(guidTemplate.WithPostFix(Postfix.List));
            @this.Map<Dictionary<string, TType>>(guidTemplate.WithPostFix(Postfix.StringDictionary));
            return @this;
        }
    }
}
