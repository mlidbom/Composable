using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventSourcing.EventRefactoring;
using Composable.CQRS.EventSourcing.EventRefactoring.Naming;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using log4net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("No longer supported at all. Please use MicrosoftSqlServerEventStore. Only still in the assembly for binary compatibility.", error: true)]
    public class SqlServerEventStore : IEventStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));

        public readonly string ConnectionString;
        private readonly ISingleContextUseGuard _usageGuard;

        private readonly SqlServerEventStoreEventReader _eventReader;
        private readonly SqlServerEventStoreEventWriter _eventWriter;
        private readonly SqlServerEventStoreEventsCache _cache;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;

        public SqlServerEventStore(string connectionString, ISingleContextUseGuard usageGuard, IEventNameMapper nameMapper = null)
        {
            Log.Debug("Constructor called");

            nameMapper = nameMapper ?? new DefaultEventNameMapper();

            ConnectionString = connectionString;
            _usageGuard = usageGuard;
            var eventSerializer = new SqlServerEvestStoreEventSerializer();
            _cache = SqlServerEventStoreEventsCache.ForConnectionString(connectionString);
            var connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
            _schemaManager = new SqlServerEventStoreSchemaManager(connectionString, nameMapper);
            _eventReader = new SqlServerEventStoreEventReader(connectionMananger, _schemaManager);
            _eventWriter = new SqlServerEventStoreEventWriter(connectionMananger, eventSerializer, _schemaManager);
        }


        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            var cachedAggregateHistory = _cache.GetCopy(aggregateId);

            cachedAggregateHistory.AddRange(
                _eventReader.GetAggregateHistory(
                    aggregateId: aggregateId,
                    startAfterVersion: cachedAggregateHistory.Count,
                    suppressTransactionWarning: true));

            //Should within a transaction a process write events, read them, then fail to commit we will have cached events that are not persisted unless we refuse to cache them here.
            if (!_aggregatesWithEventsAddedByThisInstance.Contains(aggregateId))
            {
                _cache.Store(aggregateId, cachedAggregateHistory);
            }

            return cachedAggregateHistory;
        }

        public const int StreamEventsAfterEventWithIdBatchSize = 10000;

        [Obsolete("No longer supported. Use StreamEvents()", error: true)]
        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            return _eventReader.StreamEvents(StreamEventsAfterEventWithIdBatchSize);
        }


        private readonly HashSet<Guid> _aggregatesWithEventsAddedByThisInstance = new HashSet<Guid>();
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

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Contract.Requires(eventBaseType == null || (eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType)));

            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            return _eventReader.StreamAggregateIdsInCreationOrder(eventBaseType);
        }


        public void PersistMigrations() { throw new NotImplementedException(); }
        public IEnumerable<IAggregateRootEvent> StreamEvents() { throw new NotImplementedException(); }

        public static void ResetDB(string connectionString)
        {
            new SqlServerEventStoreSchemaManager(connectionString, new DefaultEventNameMapper()).ResetDB();
        }

        public void ResetDB()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _cache.Clear();
            _schemaManager.ResetDB();
        }


        public void Dispose()
        {
        }
    }
}