using System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal interface IEventTypeToIdMapper
    {
        //Review:mlidbo: Restore from object to int as soon as we can remove the compatibility layer.
        Type GetType(object id);
        object GetId(Type type);
    }
}