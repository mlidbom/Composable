using System;

namespace Composable.Refactoring.Naming
{
    interface ITypeIdMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeId);
    }
}
