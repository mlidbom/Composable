using System;
using Composable.System.Reflection;

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Naming
{
    class DefaultEventNameMapper : IEventNameMapper
    {
        public string GetName(Type eventType) => eventType.FullName;
        public Type GetType(string eventTypeName)
        {
            try
            {
                return eventTypeName.AsType();
            }
            catch(System.Reflection.TypeExtensions.FailedToFindTypeException exception)
            {
                throw new CouldNotFindTypeBasedOnName(eventTypeName, exception);
            }
        }
    }
}