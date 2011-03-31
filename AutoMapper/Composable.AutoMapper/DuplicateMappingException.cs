#region usings

using System;

#endregion

namespace Composable.AutoMapper
{
    public class DuplicateMappingException : Exception
    {
        public DuplicateMappingException(Type sourceType, Type targetType)
            : base(string.Format("Mapping from {0} to {1} already exists", sourceType, targetType))
        {
        }
    }
}