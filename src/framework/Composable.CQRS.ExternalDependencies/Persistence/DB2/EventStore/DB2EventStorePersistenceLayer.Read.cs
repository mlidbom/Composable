using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.DB2.SystemExtensions;
using IBM.Data.DB2.Core;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.DB2.EventStore
{
    partial class DB2EventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly DB2EventStoreConnectionManager _connectionManager;

        public DB2EventStorePersistenceLayer(DB2EventStoreConnectionManager connectionManager) => _connectionManager = connectionManager;

        //var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";
        static string CreateLockHint(bool takeWriteLock) => takeWriteLock ? "FOR UPDATE" : "";

        static string CreateSelectClause() =>
            $@"
SELECT {Event.EventType}, {Event.Event}, {Event.AggregateId}, {Event.EffectiveVersion}, {Event.EventId}, {Event.UtcTimeStamp}, {Event.InsertionOrder}, {Event.TargetEvent}, {Event.RefactoringType}, {Event.InsertedVersion}, {Event.ReadOrderIntegerPart}, {Event.ReadOrderFractionPart}
FROM {Event.TableName}
";

        static EventDataRow ReadDataRow(DB2DataReader eventReader)
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
                                            ReadOrder = ReadOrder.Parse($"{eventReader.GetDB2Decimal(10)}.{eventReader.GetDB2Decimal(11)}", bypassScaleTest: true),
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
            //Urgent: Even with this in place DB2 does not block the second reader from passing right by.
            if(takeWriteLock)
            {
                _connectionManager.UseCommand(command => command.SetCommandText($"select {Event.AggregateId} from AggregateLock where AggregateId = @{Event.AggregateId} for update;")
                                                                .AddParameter(Event.AggregateId, aggregateId)
                                                                .ExecuteNonQuery());
            }

            return _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                                 command => command.SetCommandText($@"

    
{CreateSelectClause()} 
WHERE {Event.AggregateId} = @{Event.AggregateId}
    AND {Event.InsertedVersion} > @CachedVersion
    AND {Event.EffectiveVersion} >= 0
ORDER BY {Event.ReadOrderIntegerPart} ASC, {Event.ReadOrderFractionPart} ASC;
")
                                                                   .AddParameter(Event.AggregateId, aggregateId)
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
WHERE ({Event.ReadOrderIntegerPart} > @{Event.ReadOrderIntegerPart} OR ({Event.ReadOrderIntegerPart} = @{Event.ReadOrderIntegerPart} AND {Event.ReadOrderFractionPart} > @{Event.ReadOrderFractionPart}))
    AND {Event.EffectiveVersion} > 0
ORDER BY {Event.ReadOrderIntegerPart} ASC, {Event.ReadOrderFractionPart} ASC
FETCH FIRST {batchSize} ROWS ONLY;
";
                                                                    return command.SetCommandText(commandText)
                                                                                  .AddParameter(Event.ReadOrderIntegerPart, DB2Type.Decimal, lastReadEventReadOrder.ToDB2DecimalIntegerPart())
                                                                                  .AddParameter(Event.ReadOrderFractionPart, DB2Type.Decimal, lastReadEventReadOrder.ToDB2DecimalFractionPart())
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
ORDER BY {Event.ReadOrderIntegerPart} ASC, {Event.ReadOrderFractionPart} ASC")
                                                                           .ExecuteReaderAndSelect(reader => new CreationEventRow(aggregateId: reader.GetGuidFromString(0), typeId: reader.GetGuidFromString(1))));
        }
    }
}
