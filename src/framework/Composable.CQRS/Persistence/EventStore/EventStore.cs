using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Serialization;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.Persistence.EventStore
{
    class EventStore : IEventStore
    {
        readonly IEventStoreSerializer _serializer;
        static readonly ILogger Log = Logger.For<EventStore>();

        readonly ISingleContextUseGuard _usageGuard;

        readonly IEventStoreEventReader _eventReader;
        readonly IEventStoreEventWriter _eventWriter;
        readonly EventCache _cache;
        readonly IEventStoreSchemaManager _schemaManager;
        readonly IReadOnlyList<IEventMigration> _migrationFactories;

        public EventStore(IEventStorePersistenceLayer persistenceLayer, IEventStoreSerializer serializer, EventCache cache, IEnumerable<IEventMigration> migrations)
        {
            _serializer = serializer;
            Log.Debug("Constructor called");

            _migrationFactories = migrations.ToList();

            _usageGuard = new SingleThreadUseGuard();
            _cache = cache;
            _schemaManager = persistenceLayer.SchemaManager;
            _eventReader = persistenceLayer.EventReader;
            _eventWriter = persistenceLayer.EventWriter;
        }

        public IReadOnlyList<IAggregateEvent> GetAggregateHistoryForUpdate(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);

        public IReadOnlyList<IAggregateEvent> GetAggregateHistory(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);

        IReadOnlyList<IAggregateEvent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            var cachedAggregateHistory = _cache.Get(aggregateId);

            var newEventsFromPersistenceLayer = GetAggregateEventsFromPersistenceLayer(aggregateId, takeWriteLock, cachedAggregateHistory.MaxSeenInsertedVersion);

            if(newEventsFromPersistenceLayer.Length == 0)
            {
                return cachedAggregateHistory.Events;
            }

            var newerMigratedEventsExist = newEventsFromPersistenceLayer.Where(IsRefactoringEvent).Any();

            var cachedMigratedHistoryExists = cachedAggregateHistory.MaxSeenInsertedVersion > 0;

            var migrationsHaveBeenPersistedWhileWeHeldEventsInCache = cachedMigratedHistoryExists && newerMigratedEventsExist;
            if(migrationsHaveBeenPersistedWhileWeHeldEventsInCache)
            {
                _cache.Remove(aggregateId);
                // ReSharper disable once TailRecursiveCall clarity over micro optimizations any day.
                return GetAggregateHistoryInternal(aggregateId, takeWriteLock);
            }

            var newAggregateHistory = cachedAggregateHistory.Events.Count == 0
                                        ? SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newEventsFromPersistenceLayer)
                                        : cachedAggregateHistory.Events.Concat(newEventsFromPersistenceLayer)
                                                                .ToArray();


            if(cachedMigratedHistoryExists)
            {
                SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newAggregateHistory);
            }

            var maxSeenInsertedVersion =  newEventsFromPersistenceLayer.Max(@event => @event.InsertedVersion);
            _cache.Store(aggregateId, new EventCache.Entry(events: newAggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

            return newAggregateHistory;
        }

        AggregateEvent HydrateEvent(EventReadDataRow eventDataRowRow)
        {
            var @event = (AggregateEvent)_serializer.Deserialize(eventType: _schemaManager.IdMapper.GetType(eventDataRowRow.EventType), json: eventDataRowRow.EventJson);
            @event.AggregateId = eventDataRowRow.AggregateId;
            @event.AggregateVersion = eventDataRowRow.AggregateVersion;
            @event.EventId = eventDataRowRow.EventId;
            @event.UtcTimeStamp = eventDataRowRow.UtcTimeStamp;
            @event.InsertionOrder = eventDataRowRow.InsertionOrder;
            @event.InsertAfter = eventDataRowRow.InsertAfter;
            @event.InsertBefore = eventDataRowRow.InsertBefore;
            @event.Replaces = eventDataRowRow.Replaces;
            @event.InsertedVersion = eventDataRowRow.InsertedVersion;
            @event.ManualVersion = eventDataRowRow.ManualVersion;
            @event.EffectiveVersion = eventDataRowRow.EffectiveVersion;

            return @event;
        }

        AggregateEvent[] GetAggregateEventsFromPersistenceLayer(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
            => _eventReader.GetAggregateHistory(aggregateId: aggregateId,
                                                startAfterInsertedVersion: startAfterInsertedVersion,
                                                takeWriteLock: takeWriteLock)
                           .Select(HydrateEvent)
                           .ToArray();

        static bool IsRefactoringEvent(AggregateEvent @event) => @event.InsertBefore.HasValue || @event.InsertAfter.HasValue || @event.Replaces.HasValue;

        IEnumerable<IAggregateEvent> StreamEvents(int batchSize)
        {
            var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
            return streamMutator.Mutate(_eventReader.StreamEvents(batchSize).Select(HydrateEvent));
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateEvent>> handleEvents)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            _schemaManager.IdMapper.LoadTypesFromDatabase();

            var batches = StreamEvents(batchSize)
                .ChopIntoSizesOf(batchSize)
                .Select(batch => batch.ToList());
            foreach (var batch in batches)
            {
                handleEvents(batch);
            }
        }

        public void SaveEvents(IEnumerable<IAggregateEvent> events)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            events = events.ToList();
            var updatedAggregates = events.Select(@event => @event.AggregateId).Distinct().ToList();

            var eventRows = events.Cast<AggregateEvent>()
                                  .Select(@this => new EventWriteDataRow(@event: @this, eventAsJson: _serializer.Serialize(@this)))
                                  .ToList();
            _eventWriter.Insert(eventRows);
            //todo: move this to the event store updater.
            foreach(var aggregateId in updatedAggregates)
            {
                var completeAggregateHistory = _cache.Get(aggregateId)
                                                     .Events.Concat(events.Where(@event => @event.AggregateId == aggregateId))
                                                     .Cast<AggregateEvent>()
                                                     .ToArray();
                SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeAggregateHistory);

                _cache.Store(aggregateId, new EventCache.Entry(completeAggregateHistory, completeAggregateHistory.Max(@event => @event.InsertedVersion)));
            }
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            _cache.Remove(aggregateId);
            _eventWriter.DeleteAggregate(aggregateId);
        }



        public void PersistMigrations()
        {
            Log.Warning("Starting to persist migrations");

            long migratedAggregates = 0;
            long updatedAggregates = 0;
            long newEventCount = 0;
            var logInterval = 1.Minutes();
            var lastLogTime = DateTime.Now;

            const int recoverableErrorRetriesToMake = 5;

            var aggregateIdsInCreationOrder = StreamAggregateIdsInCreationOrder().ToList();

            foreach (var aggregateId in aggregateIdsInCreationOrder)
            {
                try
                {
                    var succeeded = false;
                    var retries = 0;
                    while(!succeeded)
                    {
                        try
                        {
                            //performance: bug: Look at ways to avoid taking a lock for a long time as we do now. This might be a problem in production.
                            using var transaction = new TransactionScope(TransactionScopeOption.Required, scopeTimeout: 10.Minutes());
                            var original = GetAggregateEventsFromPersistenceLayer(aggregateId: aggregateId, takeWriteLock: true);

                            var startInsertingWithVersion = original.Max(@event => @event.InsertedVersion) + 1;

                            var updatedAggregatesBeforeMigrationOfThisAggregate = updatedAggregates;

                            SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                                _migrationFactories,
                                original,
                                newEvents =>
                                {
                                    //Make sure we don't try to insert into an occupied InsertedVersion
                                    newEvents.ForEach(@event => @event.InsertedVersion = startInsertingWithVersion++);
                                    //Save all new events so they get an InsertionOrder for the next refactoring to work with in case it acts relative to any of these events
                                    var eventRows = newEvents
                                                   .Select(@this => new EventWriteDataRow(@event: @this, eventAsJson: _serializer.Serialize(@this)))
                                                   .ToList();

                                    _eventWriter.InsertRefactoringEvents(eventRows);
                                    updatedAggregates = updatedAggregatesBeforeMigrationOfThisAggregate + 1;
                                    newEventCount += newEvents.Count;
                                });

                            transaction.Complete();

                            migratedAggregates++;
                            succeeded = true;
                        }
                        catch(Exception e) when(IsRecoverableSqlException(e) && ++retries <= recoverableErrorRetriesToMake)
                        {
                            Log.Warning(e, $"Failed to persist migrations for aggregate: {aggregateId}. Exception appears to be recoverable so running retry {retries} out of {recoverableErrorRetriesToMake}");
                        }
                    }
                }
                catch(Exception exception)
                {
                    Log.Error(exception, $"Failed to persist migrations for aggregate: {aggregateId}");
                }

                if(logInterval < DateTime.Now - lastLogTime)
                {
                    lastLogTime = DateTime.Now;
                    // ReSharper disable once AccessToModifiedClosure
                    int PercentDone() => (int)(((double)migratedAggregates / aggregateIdsInCreationOrder.Count) * 100);

                    Log.Info($"{PercentDone()}% done. Inspected: {migratedAggregates} / {aggregateIdsInCreationOrder.Count}, Updated: {updatedAggregates}, New Events: {newEventCount}");
                }
            }

            Log.Warning("Done persisting migrations.");
            Log.Info($"Inspected: {migratedAggregates} , Updated: {updatedAggregates}, New Events: {newEventCount}");

        }

        static bool IsRecoverableSqlException(Exception exception)
        {
            var message = exception.Message.ToLower();
            return message.Contains("timeout") || message.Contains("deadlock");
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventType = null)
        {
            Contract.Assert.That(eventType == null || eventType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventType),
                "eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventType)");
            _usageGuard.AssertNoContextChangeOccurred(this);

            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            return _eventReader.StreamAggregateIdsInCreationOrder(eventType);
        }

        public void Dispose()
        {
        }
    }
}