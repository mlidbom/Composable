using System;
using Composable.CQRS.EventSourcing.EventRefactoring;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal class LegacySchemaSqlServerEventStoreEventTypeToIdMapper : IEventTypeToIdMapper
    {
        private readonly IEventNameMapper _nameMapper;
        public LegacySchemaSqlServerEventStoreEventTypeToIdMapper(IEventNameMapper nameMapper) {
            _nameMapper = nameMapper;
        }

        public Type GetType(object id) { return _nameMapper.GetType((string)id); }
        public object GetId(Type type) { return _nameMapper.GetName(type); }
    }
}
