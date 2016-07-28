using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Transactions;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.EventSourcing.Refactoring.Naming;
using Composable.Logging.Log4Net;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using log4net;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    public partial class SqlServerEventStore : IEventStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));        

        public readonly string ConnectionString;
        private readonly ISingleContextUseGuard _usageGuard;

        private readonly SqlServerEventStoreEventReader _eventReader;        
        private readonly SqlServerEventStoreEventWriter _eventWriter;
        private readonly SqlServerEventStoreEventsCache _cache;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;
        private readonly IReadOnlyList<IEventMigration> _migrationFactories;

        private readonly HashSet<Guid> _aggregatesWithEventsAddedByThisInstance = new HashSet<Guid>();
        private readonly SqlServerEventStoreConnectionManager _connectionMananger;

        public SqlServerEventStore(string connectionString, ISingleContextUseGuard usageGuard, IEventNameMapper nameMapper = null, IEnumerable<IEventMigration> migrations = null)
        {
            Log.Debug("Constructor called");

            _migrationFactories = migrations?.ToList() ?? new List<IEventMigration>();
            nameMapper = nameMapper ?? new DefaultEventNameMapper();

            ConnectionString = connectionString;
            _usageGuard = usageGuard;
            var eventSerializer = new SqlServerEvestStoreEventSerializer();            
            _cache = SqlServerEventStoreEventsCache.ForConnectionString(connectionString);
            _connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
            _schemaManager = new SqlServerEventStoreSchemaManager(connectionString, nameMapper);
            _eventReader = new SqlServerEventStoreEventReader(_connectionMananger, _schemaManager);
            _eventWriter = new SqlServerEventStoreEventWriter(_connectionMananger, eventSerializer, _schemaManager);
        }

        public IEnumerable<IAggregateRootEvent> GetAggregateHistoryForUpdate(Guid aggregateId)
        {
            return GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);
        }

        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId)
        {
            return GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);
        }
        private IEnumerable<IAggregateRootEvent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            lock(AggregateLockManager.GetAggregateLockObject(aggregateId))
            {
                var cachedAggregateHistory = _cache.GetCopy(aggregateId);

                var newEventsFromDatabase = _eventReader.GetAggregateHistory(
                    aggregateId: aggregateId,
                    startAfterVersion: cachedAggregateHistory.Count,
                    suppressTransactionWarning: true,
                    takeWriteLock: takeWriteLock);

                var currentHistory = cachedAggregateHistory.Count == 0 
                                                   ? SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newEventsFromDatabase) 
                                                   : cachedAggregateHistory.Concat(newEventsFromDatabase).ToList();

                currentHistory.ForEach((@event, index) => @event.AggregateRootVersion = index + 1);

                //Should within a transaction a process write events, read them, then fail to commit we will have cached events that are not persisted unless we refuse to cache them here.
                if (!_aggregatesWithEventsAddedByThisInstance.Contains(aggregateId))
                {
                    _cache.Store(aggregateId, currentHistory);
                }

                return currentHistory;
            }
        }

        private static bool IsRefactoringEvent(AggregateRootEvent @event)
        {
            return @event.InsertAfter != null || @event.InsertBefore != null || @event.Replaces != null;
        }

        public const int StreamEventsBatchSize = 10000;
       
        private IEnumerable<IAggregateRootEvent> StreamEvents()
        {            
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions();

            var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
            return streamMutator.Mutate(_eventReader.StreamEvents(StreamEventsBatchSize));
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateRootEvent>> handleEvents)
        {
            var batches = StreamEvents()
                .ChopIntoSizesOf(batchSize)
                .Select(batch => batch.ToList());
            foreach (var batch in batches)
            {
                handleEvents(batch);
            }
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            events = events.ToList();
            _aggregatesWithEventsAddedByThisInstance.AddRange(events.Select(e => e.AggregateRootId));
            _eventWriter.Insert(events.Cast<AggregateRootEvent>());
        }

        public void DeleteEvents(Guid aggregateId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            _cache.Remove(aggregateId);
            _eventWriter.DeleteAggregate(aggregateId);            
        }


        public void PersistMigrations()
        {
            this.Log().Warn($"Starting to persist migrations");

            long migratedAggregates = 0;
            long updatedAggregates = 0;
            long newEventCount = 0;
            var logInterval = 1.Minutes();
            var lastLogTime = DateTime.Now;

            foreach(var aggregateId in StreamAggregateIdsInCreationOrder())
            {                
                using(var transaction = new TransactionScope())
                {
                    lock (AggregateLockManager.GetAggregateLockObject(aggregateId))
                    {
                        var original = _eventReader.GetAggregateHistory(aggregateId: aggregateId, takeWriteLock: true).ToList();

                        var startInsertingWithVersion = original.Max(@event => @event.InsertedVersion) + 1;

                        SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, original,
                                                                                                       newEvents =>
                                                                                                       {
                                                                                                           newEvents.ForEach(@event => ((AggregateRootEvent)@event).AggregateRootVersion = startInsertingWithVersion++);
                                                                                                           SaveEvents(newEvents);
                                                                                                           updatedAggregates++;
                                                                                                           newEventCount += newEvents.Count();
                                                                                                       });
                        transaction.Complete();
                    }
                    migratedAggregates++;
                }

                if(logInterval < DateTime.Now - lastLogTime)
                {
                    this.Log().Info($"Aggregates: {migratedAggregates}, Updated: {updatedAggregates}, New Events: {newEventCount}");
                }
            }

            this.Log().Info($"Aggregates: {migratedAggregates}, Updated: {updatedAggregates}, New Events: {newEventCount}");

            EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions();

            this.Log().Warn($"Done persisting migrations.");
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            Contract.Assert(eventBaseType == null || (eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType)));
            _usageGuard.AssertNoContextChangeOccurred(this);            

            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions();
            return _eventReader.StreamAggregateIdsInCreationOrder(eventBaseType);
        }        

        private void EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions()
        {
            this.Log().Debug($"{nameof(EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions)}: Starting");

            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandText = SqlStatements.EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions;
                    command.ExecuteNonQuery();
                });

            this.Log().Debug($"{nameof(EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions)}: Done");
        }

        public static void ResetDB(string connectionString)
        {
            new SqlServerEventStore(connectionString, new SingleThreadUseGuard()).ResetDB();
        }

        public void ResetDB()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            ClearCache();
            _schemaManager.ResetDB();           
        }

        public void ClearCache()
        {
            _cache.Clear();
        }


        public void Dispose()
        {
        }
    }
}