using System;
using Composable.System;

namespace Composable.KeyValueStorage
{
    public class NoSuchDocumentException : Exception
    {
        public NoSuchDocumentException(Guid key, Type type):base("Type: {0}, Key: {1}".FormatWith(type.FullName, key))
        {
        }
    }
}