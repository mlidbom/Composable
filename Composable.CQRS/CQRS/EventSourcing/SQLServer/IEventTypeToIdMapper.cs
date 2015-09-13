using System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal interface IEventTypeToIdMapper
    {
        //Review:mlidbo: Restore from object to int as soon as we can remove the compatibility layer.
        Type GetType(object id);
        object GetId(Type type);
    }
}