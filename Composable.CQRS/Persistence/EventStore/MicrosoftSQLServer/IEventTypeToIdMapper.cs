using System;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    interface IEventTypeToIdMapper
    {
        Type GetType(int id);
        int GetId(Type type);
    }
}