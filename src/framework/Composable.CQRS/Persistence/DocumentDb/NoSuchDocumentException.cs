using System;

namespace Composable.Persistence.DocumentDb
{
    public class NoSuchDocumentException : Exception
    {
        public NoSuchDocumentException(object key, Type type):base($"Type: {type.FullName}, Key: {key}")
        {
        }

        public NoSuchDocumentException(object key, Guid type) : base($"TypeId.Guid: {type}, Key: {key}")
        {
        }
    }
}