using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.EventStore.PersistenceLayer;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.MsSql.EventStore
{
    partial class MsSqlEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly MsSqlEventStoreConnectionManager _connectionManager;

        public MsSqlEventStorePersistenceLayer(MsSqlEventStoreConnectionManager connectionManager) => _connectionManager = connectionManager;

        static string CreateSelectClause(bool takeWriteLock) => InternalSelect(takeWriteLock: takeWriteLock);
        static string CreateSelectTopClause(int top, bool takeWriteLock) => InternalSelect(top: top, takeWriteLock: takeWriteLock);

        static string InternalSelect(bool takeWriteLock, int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            //todo: Ensure that READCOMMITTED is truly sane here. If so add a comment describing why and why using it is a good idea.
            var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";

            return $@"
SELECT {topClause} 
{Event.EventType}, {Event.Event}, {Event.AggregateId}, {Event.EffectiveVersion}, {Event.EventId}, {Event.UtcTimeStamp}, {Event.InsertionOrder}, {Event.TargetEvent}, {Event.RefactoringType}, {Event.InsertedVersion}, {Event.ReadOrder}
FROM {Event.TableName} {lockHint} ";
        }

        static EventDataRow ReadDataRow(SqlDataReader eventReader) => new EventDataRow(
            eventType: eventReader.GetGuid(0),
            eventJson: eventReader.GetString(1),
            eventId: eventReader.GetGuid(4),
            aggregateVersion: eventReader.GetInt32(3),
            aggregateId: eventReader.GetGuid(2),
            //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
            utcTimeStamp: DateTime.SpecifyKind(eventReader.GetDateTime(5), DateTimeKind.Utc),
            storageInformation: new AggregateEventStorageInformation()
                                    {
                                        ReadOrder = ReadOrder.FromSqlDecimal(eventReader.GetSqlDecimal(10)),
                                        InsertedVersion = eventReader.GetInt32(9),
                                        EffectiveVersion = eventReader.GetInt32(3),
                                        RefactoringInformation = (eventReader[7] as Guid?, eventReader[8] as byte?)switch
                                        {
                                            (null, null) => null,
                                            (Guid targetEvent, byte type) => new AggregateEventRefactoringInformation(targetEvent, (AggregateEventRefactoringType)type),
                                            _ => throw new Exception("Should not be possible to get here")
                                        }
                                    }
        );

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) =>
            _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                          command => command.SetCommandText($@"
{CreateSelectClause(takeWriteLock)} 
WHERE {Event.AggregateId} = @{Event.AggregateId}
    AND {Event.InsertedVersion} > @CachedVersion
    AND {Event.EffectiveVersion} > 0
ORDER BY {Event.ReadOrder} ASC")
                                                            .AddParameter(Event.AggregateId, aggregateId)
                                                            .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                            .ExecuteReaderAndSelect(ReadDataRow)
                                                            .ToList());

        public IEnumerable<EventDataRow> StreamEvents(int batchSize)
        {
            SqlDecimal lastReadEventReadOrder = 0;
            int fetchedInThisBatch;
            do
            {
                var historyData = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                                command => command.SetCommandText($@"
{CreateSelectTopClause(batchSize, takeWriteLock: false)} 
WHERE {Event.ReadOrder}  > @{Event.ReadOrder}
    AND {Event.EffectiveVersion} > 0
ORDER BY {Event.ReadOrder} ASC")
                                                                                  .AddParameter(Event.ReadOrder, SqlDbType.Decimal, lastReadEventReadOrder)
                                                                                  .ExecuteReaderAndSelect(ReadDataRow)
                                                                                  .ToList());
                if(historyData.Any())
                {
                    lastReadEventReadOrder = historyData[^1].StorageInformation.ReadOrder!.Value.ToSqlDecimal();
                }

                //We do not yield while reading from the reader since that may cause code to run that will cause another sql call into the same connection. Something that throws an exception unless you use an unusual and non-recommended connection string setting.
                foreach(var eventDataRow in historyData)
                {
                    yield return eventDataRow;
                }

                fetchedInThisBatch = historyData.Count;
            } while(!(fetchedInThisBatch < batchSize));
        }

        public IReadOnlyList<CreationEventRow> ListAggregateIdsInCreationOrder()
        {
            return _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                 action: command => command.SetCommandText($@"
SELECT {Event.AggregateId}, {Event.EventType} 
FROM {Event.TableName} 
WHERE {Event.EffectiveVersion} = 1 
ORDER BY {Event.ReadOrder} ASC")
                                                                           .ExecuteReaderAndSelect(reader => new CreationEventRow(aggregateId: reader.GetGuid(0), typeId: reader.GetGuid(1))));
        }
    }
}
