using System;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    public class CouldNotFindTypeBasedOnName : Exception
    {
        public CouldNotFindTypeBasedOnName(string typeName) : base(CreateMessage(typeName)) { }
        public CouldNotFindTypeBasedOnName(string typeName, Exception innerException) : base(CreateMessage(typeName), innerException) { }
        static string CreateMessage(string typeName) { return $"Failed to find a type for: {typeName}"; }
    }
}
