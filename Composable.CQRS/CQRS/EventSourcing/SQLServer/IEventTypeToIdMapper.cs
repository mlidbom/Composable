using System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal interface IEventTypeToIdMapper
    {
        Type GetType(object id);
        object GetId(Type type);
    }
}