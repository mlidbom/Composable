using System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal interface IEventTypeToIdMapper
    {
        Type GetType(int id);
        int GetId(Type type);
    }
}