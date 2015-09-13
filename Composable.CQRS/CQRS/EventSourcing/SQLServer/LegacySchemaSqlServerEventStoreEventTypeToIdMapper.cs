using System;
using Composable.CQRS.EventSourcing.EventRefactoring;

namespace Composable.CQRS.EventSourcing.SQLServer
{
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
