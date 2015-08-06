using System.Data.SqlClient;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEventStoreEventReader
    {
        public string SelectClause => InternalSelect();
        public string SelectTopClause(int top) => InternalSelect(top);

        private string InternalSelect(int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            return $@"
SELECT {topClause} {EventTable.Columns.EventType}, {EventTable.Columns.Event}, {EventTable.Columns.AggregateId}, {EventTable.Columns.AggregateVersion}, {EventTable.Columns.EventId}, {EventTable.Columns.TimeStamp} 
FROM {EventTable.Name} With(UPDLOCK, READCOMMITTED, ROWLOCK) ";
        }

        private static readonly SqlServerEvestStoreEventSerializer EventSerializer = new SqlServerEvestStoreEventSerializer();

        public IAggregateRootEvent Read(SqlDataReader eventReader)
        {
            var @event = EventSerializer.Deserialize(eventReader.GetString(0), eventReader.GetString(1));
            @event.AggregateRootId = eventReader.GetGuid(2);
            @event.AggregateRootVersion = eventReader.GetInt32(3);
            @event.EventId = eventReader.GetGuid(4);
            @event.TimeStamp = eventReader.GetDateTime(5);

            return @event;
        }

    }
}