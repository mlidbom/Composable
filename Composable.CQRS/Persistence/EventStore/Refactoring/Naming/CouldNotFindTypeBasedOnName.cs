using System;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    class CouldNotFindTypeBasedOnName : Exception
    {
        public CouldNotFindTypeBasedOnName(string typeName) : base(CreateMessage(typeName)) { }
        public CouldNotFindTypeBasedOnName(string typeName, Exception innerException) : base(CreateMessage(typeName), innerException) { }
        static string CreateMessage(string typeName) => $"Failed to find a type for: {typeName}";
    }
}
