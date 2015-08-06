using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEventStoreEventWriter
    {
        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        private readonly SqlServerEvestStoreEventSerializer _eventSerializer;

        public SqlServerEventStoreEventWriter(SqlServerEventStoreConnectionManager connectionMananger, SqlServerEvestStoreEventSerializer eventSerializer)
        {
            _connectionMananger = connectionMananger;
            _eventSerializer = eventSerializer;
        }

        public void Insert(IEnumerable<IAggregateRootEvent> events)
        {
            using(var connection = _connectionMananger.OpenConnection())
            {
                foreach(var @event in events)
                {
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;

                        command.CommandText +=
                            $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.AggregateVersion},  {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.TimeStamp},  {EventTable.Columns.Event}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.AggregateVersion}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.TimeStamp}, @{EventTable.Columns.Event})";

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateVersion, @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType, @event.GetType().FullName));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, @event.EventId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.TimeStamp, @event.TimeStamp));

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, _eventSerializer.Serialize(@event)));

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteEvents(Guid aggregateId)
        {
            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText +=
                        $"DELETE {EventTable.Name} With(ROWLOCK) WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, aggregateId));
                    command.ExecuteNonQuery();
                });
        }
    }
}