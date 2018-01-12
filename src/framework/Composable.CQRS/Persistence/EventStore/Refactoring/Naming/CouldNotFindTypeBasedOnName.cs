using System;

namespace Composable.Persistence.EventStore.Refactoring.Naming
{
    class CouldNotFindTypeBasedOnName : Exception
    {
        public CouldNotFindTypeBasedOnName(string typeName) : base(CreateMessage(typeName)) { }
        static string CreateMessage(string typeName) => $"Failed to find a type for: {typeName}";
    }
}
