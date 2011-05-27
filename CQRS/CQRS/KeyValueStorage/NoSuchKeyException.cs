using System;
using Composable.System;

namespace Composable.KeyValueStorage
{
    public class NoSuchKeyException : Exception
    {
        public NoSuchKeyException(Guid key, Type type):base("Type: {0}, Key: {1}".FormatWith(type.FullName, key))
        {
        }
    }
}