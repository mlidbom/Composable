using System;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    internal interface IEventTypeToIdMapper
    {
        Type GetType(int id);
        int GetId(Type type);
    }
}