using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.SystemExtensions;
using Oracle.ManagedDataAccess.Client;

using C = Composable.Persistence.Common.EventStore.EventTable.Columns;

namespace Composable.Persistence.Oracle.EventStore
{
    partial class OracleEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly OracleEventStoreConnectionManager _connectionManager;

        public OracleEventStorePersistenceLayer(OracleEventStoreConnectionManager connectionManager) => _connectionManager = connectionManager;

        //var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";
        static string CreateLockHint(bool takeWriteLock) => takeWriteLock ? "FOR UPDATE" : "";

        static string CreateSelectClause() =>
            $@"
SELECT {C.EventType}, {C.Event}, {C.AggregateId}, {C.EffectiveVersion}, {C.EventId}, {C.UtcTimeStamp}, {C.InsertionOrder}, {C.TargetEvent}, {C.RefactoringType}, {C.InsertedVersion}, {C.ReadOrder}
FROM {EventTable.Name}
";

        static EventDataRow ReadDataRow(OracleDataReader eventReader)
        {
            return new EventDataRow(
                eventType: eventReader.GetGuidFromString(0),
                eventJson: eventReader.GetString(1),
                eventId: eventReader.GetGuidFromString(4),
                aggregateVersion: eventReader.GetInt32(3),
                aggregateId: eventReader.GetGuidFromString(2),
                //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
                utcTimeStamp: DateTime.SpecifyKind(eventReader.GetDateTime(5), DateTimeKind.Utc),
                storageInformation: new AggregateEventStorageInformation()
                                        {
                                            ReadOrder = eventReader.GetOracleDecimal(10).ToReadOrder(),
                                            InsertedVersion = eventReader.GetInt32(9),
                                            EffectiveVersion = eventReader.GetInt32(3),
                                            RefactoringInformation = (eventReader[7] as string, eventReader[8] as short?)switch
                                            {
                                                (null, null) => null,
                                                (string targetEvent, short type) => new AggregateEventRefactoringInformation(Guid.Parse(targetEvent), (AggregateEventRefactoringType)type),
                                                _ => throw new Exception("Should not be possible to get here")
                                            }
                                        }
            );
        }

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
        {
            if(takeWriteLock)
            {
                //Performance: Find a way of doing this so that it does not involve two round trips to the server. If running as single-instance we can use in-memory transactional locking such as in the InMemory Persistence Layer to avoid needing this.
                //Without this hack Oracle does not correctly serialize access to aggregates and odds are you would get a lot of failed transactions if an aggregate is "popular"
                _connectionManager.UseCommand(command => command.SetCommandText($"select {C.AggregateId} from AggregateLock where AggregateId = :{C.AggregateId} for update")
                                                                .AddParameter(C.AggregateId, aggregateId)
                                                                .ExecuteNonQuery());
            }

            return _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                                 command => command.SetCommandText($@"
{CreateSelectClause()} 
WHERE {C.AggregateId} = :{C.AggregateId}
    AND {C.InsertedVersion} > :CachedVersion
    AND {C.EffectiveVersion} >= 0
ORDER BY {C.ReadOrder} ASC")
                                                                   .AddParameter(C.AggregateId, aggregateId)
                                                                   .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                                   .ExecuteReaderAndSelect(ReadDataRow)
                                                                   .ToList());
        }

        public IEnumerable<EventDataRow> StreamEvents(int batchSize)
        {
            ReadOrder lastReadEventReadOrder = default;
            int fetchedInThisBatch;
            do
            {
                var historyData = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                                command =>
                                                                {
                                                                    var commandText = $@"
{CreateSelectClause()} 
WHERE {C.ReadOrder}  > :{C.ReadOrder}
    AND {C.EffectiveVersion} > 0
    AND ROWNUM <= {batchSize}
ORDER BY {C.ReadOrder} ASC";
                                                                    return command.SetCommandText(commandText)
                                                                                  .AddParameter(C.ReadOrder, OracleDbType.Decimal, lastReadEventReadOrder.ToOracleDecimal())
                                                                                  .ExecuteReaderAndSelect(ReadDataRow)
                                                                                  .ToList();
                                                                });
                if(historyData.Any())
                {
                    lastReadEventReadOrder = historyData[^1].StorageInformation.ReadOrder!.Value;
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
SELECT {C.AggregateId}, {C.EventType} 
FROM {EventTable.Name} 
WHERE {C.EffectiveVersion} = 1 
ORDER BY {C.ReadOrder} ASC")
                                                                           .ExecuteReaderAndSelect(reader => new CreationEventRow(aggregateId: reader.GetGuidFromString(0), typeId: reader.GetGuidFromString(1))));
        }
    }
}
