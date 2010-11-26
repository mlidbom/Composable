using System;
using Composable.System;

namespace Composable.AutoMapper
{
    public class DuplicateMappingException : Exception
    {
        public DuplicateMappingException(Type sourceType, Type targetType):base("Mapping from {0} to {1} already exists".FormatWith(sourceType, targetType))
        {
            
        }
    }
}