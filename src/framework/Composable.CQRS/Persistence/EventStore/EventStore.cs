using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;

namespace Composable.Persistence.EventStore
{
    class EventStore : IEventStore
    {
        readonly ITypeMapper _typeMapper;
        readonly IEventStoreSerializer _serializer;
        static readonly ILogger Log = Logger.For<EventStore>();

        readonly SingleThreadUseGuard _usageGuard;

        readonly IEventStorePersistenceLayer _persistenceLayer;

        readonly EventCache _cache;
        readonly IReadOnlyList<IEventMigration> _migrationFactories;

        public EventStore(IEventStorePersistenceLayer persistenceLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache, IEnumerable<IEventMigration> migrations)
        {
            _typeMapper = typeMapper;
            _serializer = serializer;
            Log.Debug("Constructor called");

            _migrationFactories = migrations.ToList();

            _usageGuard = new SingleThreadUseGuard();
            _cache = cache;
            _persistenceLayer = persistenceLayer;
        }

        public IReadOnlyList<IAggregateEvent> GetAggregateHistoryForUpdate(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);

        public IReadOnlyList<IAggregateEvent> GetAggregateHistory(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);

        IReadOnlyList<IAggregateEvent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();

            var cachedAggregateHistory = _cache.Get(aggregateId);

            var newHistoryFromPersistenceLayer = GetAggregateEventsFromPersistenceLayer(aggregateId, takeWriteLock, cachedAggregateHistory.MaxSeenInsertedVersion);

            if(newHistoryFromPersistenceLayer.Length == 0)
            {
                return cachedAggregateHistory.Events;
            }

            var newerMigratedEventsExist = newHistoryFromPersistenceLayer.Where(IsRefactoringEvent).Any();

            var cachedMigratedHistoryExists = cachedAggregateHistory.MaxSeenInsertedVersion > 0;

            var migrationsHaveBeenPersistedWhileWeHeldEventsInCache = cachedMigratedHistoryExists && newerMigratedEventsExist;
            if(migrationsHaveBeenPersistedWhileWeHeldEventsInCache)
            {
                _cache.Remove(aggregateId);
                // ReSharper disable once TailRecursiveCall clarity over micro optimizations any day.
                return GetAggregateHistoryInternal(aggregateId, takeWriteLock);
            }

            var newEventsFromPersistenceLayer = newHistoryFromPersistenceLayer.Select(@this => @this.Event).ToArray();
            if(cachedAggregateHistory.Events.Count == 0)
            {
                AggregateHistoryValidator.ValidateHistory(aggregateId, newEventsFromPersistenceLayer);
            }

            var newAggregateHistory = cachedAggregateHistory.Events.Count == 0
                                        ? SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newEventsFromPersistenceLayer)
                                        : cachedAggregateHistory.Events.Concat(newEventsFromPersistenceLayer)
                                                                .ToArray();


            if(cachedMigratedHistoryExists)
            {
                SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newAggregateHistory);
            }

            var maxSeenInsertedVersion =  newHistoryFromPersistenceLayer.Max(@event => @event.StorageInformation.InsertedVersion);
            AggregateHistoryValidator.ValidateHistory(aggregateId, newAggregateHistory);
            _cache.Store(aggregateId, new EventCache.Entry(events: newAggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

            return newAggregateHistory;
        }

        AggregateEvent HydrateEvent(EventDataRow eventDataRowRow)
        {
            var @event = (AggregateEvent)_serializer.Deserialize(eventType: _typeMapper.GetType(new TypeId(eventDataRowRow.EventType)), json: eventDataRowRow.EventJson);
            @event.AggregateId = eventDataRowRow.AggregateId;
            @event.AggregateVersion = eventDataRowRow.AggregateVersion;
            @event.EventId = eventDataRowRow.EventId;
            @event.UtcTimeStamp = eventDataRowRow.UtcTimeStamp;
            return @event;
        }

        AggregateEventWithRefactoringInformation[] GetAggregateEventsFromPersistenceLayer(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
            => _persistenceLayer.GetAggregateHistory(aggregateId: aggregateId,
                                                startAfterInsertedVersion: startAfterInsertedVersion,
                                                takeWriteLock: takeWriteLock)
                           .Select(@this => new AggregateEventWithRefactoringInformation(HydrateEvent(@this), @this.StorageInformation) )
                           .ToArray();

        static bool IsRefactoringEvent(AggregateEventWithRefactoringInformation @event) => @event.StorageInformation.RefactoringInformation != null;

        IEnumerable<IAggregateEvent> StreamEvents(int batchSize)
        {
            var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
            return streamMutator.Mutate(_persistenceLayer.StreamEvents(batchSize).Select(HydrateEvent));
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateEvent>> handleEvents)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();

            var batches = StreamEvents(batchSize)
                .ChopIntoSizesOf(batchSize)
                .Select(batch => batch.ToList());
            foreach (var batch in batches)
            {
                handleEvents(batch);
            }
        }

        public void SaveSingleAggregateEvents(IReadOnlyList<IAggregateEvent> aggregateEvents)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();

            var aggregateId = aggregateEvents.First().AggregateId;

            if(aggregateEvents.Any(@this => @this.AggregateId != aggregateId))
            {
                throw new ArgumentException("Got events from multiple Aggregates. This is not supported.");
            }

            var cacheEntry = _cache.Get(aggregateId);
            var specifications = aggregateEvents.Select(@event => cacheEntry.CreateInsertionSpecificationForNewEvent(@event)).ToArray();

            var eventRows = aggregateEvents
                           .Select(@event => new EventDataRow(specification: cacheEntry.CreateInsertionSpecificationForNewEvent(@event), _typeMapper.GetId(@event.GetType()).GuidValue, eventAsJson: _serializer.Serialize((AggregateEvent)@event)))
                           .ToList();

            eventRows.ForEach(@this => @this.StorageInformation.EffectiveVersion = @this.AggregateVersion);
            _persistenceLayer.InsertSingleAggregateEvents(eventRows);

            var completeAggregateHistory = cacheEntry
                                          .Events.Concat(aggregateEvents)
                                          .Cast<AggregateEvent>()
                                          .ToArray();
            SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeAggregateHistory);
            AggregateHistoryValidator.ValidateHistory(aggregateId, completeAggregateHistory);

            _cache.Store(aggregateId, new EventCache.Entry(completeAggregateHistory,
                                                           maxSeenInsertedVersion: specifications.Max(specification => specification.InsertedVersion)));
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();
            _cache.Remove(aggregateId);
            _persistenceLayer.DeleteAggregate(aggregateId);
        }



        public void PersistMigrations()
        {
            Assert.State.Assert(Transaction.Current == null, $"Cannot run {nameof(PersistMigrations)} within a transaction. Internally manages transactions.");
            Log.Warning("Starting to persist migrations");

            long migratedAggregates = 0;
            long updatedAggregates = 0;
            long newEventCount = 0;
            var logInterval = 1.Minutes();
            var lastLogTime = DateTime.Now;

            const int recoverableErrorRetriesToMake = 5;
            var exceptions = new List<(Guid AggregateId,Exception Exception)>();

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
                            {
                                var original = GetAggregateEventsFromPersistenceLayer(aggregateId: aggregateId, takeWriteLock: true);

                                var highestSeenVersion = original.Max(@event => @event.StorageInformation.InsertedVersion) + 1;

                                var updatedAggregatesBeforeMigrationOfThisAggregate = updatedAggregates;

                                var refactorings = new List<List<EventDataRow>>();

                                var inMemoryMigratedHistory = SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                                    _migrationFactories,
                                    original.Select(@this => @this.Event).ToArray(),
                                    newEvents =>
                                    {
                                        //Make sure we don't try to insert into an occupied InsertedVersion
                                        newEvents.ForEach(refactoredEvent =>
                                        {
                                            refactoredEvent.StorageInformation.InsertedVersion = highestSeenVersion++;
                                        });

                                        refactorings.Add(newEvents
                                                        .Select(@this => new EventDataRow(@event: @this.NewEvent,
                                                                                          @this.StorageInformation,
                                                                                          _typeMapper.GetId(@this.NewEvent.GetType()).GuidValue,
                                                                                          eventAsJson: _serializer.Serialize(@this.NewEvent)))
                                                        .ToList());

                                        updatedAggregates = updatedAggregatesBeforeMigrationOfThisAggregate + 1;
                                        newEventCount += newEvents.Count;
                                    });

                                if(refactorings.Count > 0)
                                {
                                    refactorings.ForEach(InsertEventsForSingleRefactoring);

                                    FixManualVersions(original, inMemoryMigratedHistory, refactorings);

                                    var loadedAggregateHistory = GetAggregateHistoryInternal(aggregateId, takeWriteLock:false);
                                    AggregateHistoryValidator.ValidateHistory(aggregateId, loadedAggregateHistory);
                                    AssertHistoriesAreIdentical(inMemoryMigratedHistory, loadedAggregateHistory);
                                }

                                migratedAggregates++;
                                succeeded = true;
                                transaction.Complete();
                            }
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
                    exceptions.Add((aggregateId, exception));
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
            if(exceptions.Any())
            {
                throw new AggregateException($@"
Failed to persist {exceptions.Count} migrations. 

AggregateIds: 
{exceptions.Select(@this => @this.AggregateId.ToString()).Join($",{Environment.NewLine}")}", exceptions.Select(@this => @this.Exception));
            }

        }

        void FixManualVersions(AggregateEventWithRefactoringInformation[] originalHistory, AggregateEvent[] newHistory, IReadOnlyList<List<EventDataRow>> refactorings)
        {
            var versionUpdates = new List<VersionSpecification>();
            var replacedOrRemoved = originalHistory.Where(@this => newHistory.None(@event => @event.EventId == @this.Event.EventId)).ToList();
            versionUpdates.AddRange(replacedOrRemoved.Select(@this => new VersionSpecification(@this.Event.EventId, -@this.StorageInformation.EffectiveVersion)));

            var replacedOrRemoved2 = refactorings.SelectMany(@this =>@this).Where(@this => newHistory.None(@event => @event.EventId == @this.EventId));
            versionUpdates.AddRange(replacedOrRemoved2.Select(@this => new VersionSpecification(@this.EventId, -@this.StorageInformation.EffectiveVersion)));

            versionUpdates.AddRange(newHistory.Select((@this , index) => new VersionSpecification(@this.EventId, index + 1)));

            _persistenceLayer.UpdateEffectiveVersions(versionUpdates);
        }

        void AssertHistoriesAreIdentical(AggregateEvent[] inMemoryMigratedHistory, IReadOnlyList<IAggregateEvent> loadedAggregateHistory)
        {
            Assert.Result.Assert(inMemoryMigratedHistory.Length == loadedAggregateHistory.Count);
            for(int index = 0; index < inMemoryMigratedHistory.Length; ++index)
            {
                var inMemory = inMemoryMigratedHistory[index];
                var loaded = loadedAggregateHistory[index];
                Assert.Result.Assert(inMemory.AggregateId == loaded.AggregateId);
                Assert.Result.Assert(inMemory.EventId == loaded.EventId);
                Assert.Result.Assert(inMemory.AggregateVersion == loaded.AggregateVersion);
                Assert.Result.Assert(inMemory.UtcTimeStamp == loaded.UtcTimeStamp);
                Assert.Result.Assert(inMemory.GetType() == loaded.GetType());
                Assert.Result.Assert(_serializer.Serialize(inMemory) == _serializer.Serialize((AggregateEvent)loaded));
            }
        }

        void InsertEventsForSingleRefactoring(IReadOnlyList<EventDataRow> events)
        {
            var refactoring = events.First().StorageInformation.RefactoringInformation!;

            switch(refactoring.RefactoringType)
            {
                case AggregateEventRefactoringType.Replace:
                    ReplaceEvent(refactoring.TargetEvent, events.ToArray());
                    break;
                case AggregateEventRefactoringType.InsertBefore:
                    InsertBeforeEvent(refactoring.TargetEvent, events.ToArray());
                    break;
                case AggregateEventRefactoringType.InsertAfter:
                    InsertAfterEvent(refactoring.TargetEvent, events.ToArray());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void InsertAfterEvent(Guid eventId, EventDataRow[] insertAfterGroup)
        {
            var eventToInsertAfter = _persistenceLayer.LoadEventNeighborHood(eventId);

            SetManualReadOrders(newEvents: insertAfterGroup,
                                rangeStart: eventToInsertAfter.EffectiveReadOrder,
                                rangeEnd: eventToInsertAfter.NextEventReadOrder);

            _persistenceLayer.InsertSingleAggregateEvents(insertAfterGroup);
        }

        void InsertBeforeEvent(Guid eventId, EventDataRow[] insertBefore)
        {
            var eventToInsertBefore = _persistenceLayer.LoadEventNeighborHood(eventId);

            SetManualReadOrders(newEvents: insertBefore,
                                rangeStart: eventToInsertBefore.PreviousEventReadOrder,
                                rangeEnd: eventToInsertBefore.EffectiveReadOrder);

            _persistenceLayer.InsertSingleAggregateEvents(insertBefore);
        }

        void ReplaceEvent(Guid eventId, EventDataRow[] replacementEvents)
        {
            var eventToReplace = _persistenceLayer.LoadEventNeighborHood(eventId);

            SetManualReadOrders(newEvents: replacementEvents,
                                rangeStart: eventToReplace.EffectiveReadOrder,
                                rangeEnd: eventToReplace.NextEventReadOrder);

            _persistenceLayer.InsertSingleAggregateEvents(replacementEvents);
        }

        static void SetManualReadOrders(EventDataRow[] newEvents, ReadOrder rangeStart, ReadOrder rangeEnd)
        {
            var readOrders = ReadOrder.CreateOrdersForEventsBetween(newEvents.Length, rangeStart, rangeEnd);
            for (int index = 0; index < newEvents.Length; index++)
            {
                newEvents[index].StorageInformation.ReadOrder = readOrders[index];
            }
        }

        static bool IsRecoverableSqlException(Exception exception)
        {
            var message = exception.Message.ToLower();
            return message.Contains("timeout") || message.Contains("deadlock");
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null)
        {
            Contract.Assert.That(eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventBaseType),
                                 "eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventType)");
            _usageGuard.AssertNoContextChangeOccurred(this);

            _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();
            return _persistenceLayer.ListAggregateIdsInCreationOrder()
                                    .Where(@this => eventBaseType == null || eventBaseType.IsAssignableFrom(_typeMapper.GetType(new TypeId(@this.TypeId))))
                                    .Select(@this => @this.AggregateId);
        }

        public void Dispose()
        {
        }

        class AggregateEventWithRefactoringInformation
        {
            public AggregateEventWithRefactoringInformation(AggregateEvent @event, AggregateEventStorageInformation storageInformation)
            {
                Event = @event;
                StorageInformation = storageInformation;
            }

            internal AggregateEvent Event { get; }
            internal AggregateEventStorageInformation StorageInformation { get; }
        }
    }
}