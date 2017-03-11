using System;

namespace Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer
{
    interface IEventTypeToIdMapper
    {
        Type GetType(int id);
        int GetId(Type type);
    }
}