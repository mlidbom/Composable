using System;
using Composable.System;

namespace Composable.KeyValueStorage
{
    class NoSuchDocumentException : Exception
    {
        public NoSuchDocumentException(object key, Type type):base("Type: {0}, Key: {1}".FormatWith(type.FullName, key))
        {
        }
    }
}