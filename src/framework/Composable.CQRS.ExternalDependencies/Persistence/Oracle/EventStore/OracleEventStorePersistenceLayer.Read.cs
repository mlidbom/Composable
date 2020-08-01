using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.SystemExtensions;
using Oracle.ManagedDataAccess.Client;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

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
SELECT {Event.EventType}, {Event.Event}, {Event.AggregateId}, {Event.EffectiveVersion}, {Event.EventId}, {Event.UtcTimeStamp}, {Event.InsertionOrder}, {Event.TargetEvent}, {Event.RefactoringType}, {Event.InsertedVersion}, {Event.ReadOrder}
FROM {Event.TableName}
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
            return _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                                 command => command.SetCommandText($@"
DECLARE
     existing_aggregate_id {Lock.TableName}.{Lock.AggregateId}%TYPE;
BEGIN

    IF (:TakeWriteLock) THEN
        BEGIN
            select  {Lock.AggregateId} INTO existing_aggregate_id from AggregateLock where {Lock.AggregateId} = :{Lock.AggregateId} for update;
            EXCEPTION
                WHEN NO_DATA_FOUND THEN
                existing_aggregate_id := NULL;
        END;
    END IF;
    
    OPEN :rcursor FOR {CreateSelectClause()} 
    WHERE {Event.AggregateId} = :{Event.AggregateId}
        AND {Event.InsertedVersion} > :CachedVersion
        AND {Event.EffectiveVersion} >= 0
    ORDER BY {Event.ReadOrder} ASC;

END;
")
                                                                   .AddParameter(Event.AggregateId, aggregateId)
                                                                   .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                                   .AddParameter("TakeWriteLock", OracleDbType.Boolean, takeWriteLock)
                                                                   .AddParameter(new OracleParameter(parameterName:"rcursor", type: OracleDbType.RefCursor, direction: ParameterDirection.Output))
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
WHERE {Event.ReadOrder}  > :{Event.ReadOrder}
    AND {Event.EffectiveVersion} > 0
    AND ROWNUM <= {batchSize}
ORDER BY {Event.ReadOrder} ASC";
                                                                    return command.SetCommandText(commandText)
                                                                                  .AddParameter(Event.ReadOrder, OracleDbType.Decimal, lastReadEventReadOrder.ToOracleDecimal())
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
SELECT {Event.AggregateId}, {Event.EventType} 
FROM {Event.TableName} 
WHERE {Event.EffectiveVersion} = 1 
ORDER BY {Event.ReadOrder} ASC")
                                                                           .ExecuteReaderAndSelect(reader => new CreationEventRow(aggregateId: reader.GetGuidFromString(0), typeId: reader.GetGuidFromString(1))));
        }
    }
}
