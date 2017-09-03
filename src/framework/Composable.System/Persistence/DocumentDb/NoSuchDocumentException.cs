using System;

namespace Composable.Persistence.DocumentDb
{
    class NoSuchDocumentException : Exception
    {
        public NoSuchDocumentException(object key, Type type):base($"Type: {type.FullName}, Key: {key}")
        {
        }
    }
}