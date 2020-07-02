using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

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

            var maxSeenInsertedVersion =  newHistoryFromPersistenceLayer.Max(@event => @event.RefactoringInformation.InsertedVersion);
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
                           .Select(@this => new AggregateEventWithRefactoringInformation(HydrateEvent(@this), @this.RefactoringInformation) )
                           .ToArray();

        static bool IsRefactoringEvent(AggregateEventWithRefactoringInformation @event) => @event.RefactoringInformation.InsertBefore.HasValue || @event.RefactoringInformation.InsertAfter.HasValue || @event.RefactoringInformation.Replaces.HasValue;

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

            eventRows.ForEach(@this => @this.RefactoringInformation.EffectiveVersion = @this.AggregateVersion);
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
                            var original = GetAggregateEventsFromPersistenceLayer(aggregateId: aggregateId, takeWriteLock: true);

                            var highestSeenVersion = original.Max(@event => @event.RefactoringInformation.InsertedVersion) + 1;

                            var updatedAggregatesBeforeMigrationOfThisAggregate = updatedAggregates;

                            var refactoringEvents = new List<List<EventDataRow>>();

                            var inMemoryMigratedHistory = SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                                _migrationFactories,
                                original.Select(@this => @this.Event).ToArray(),
                                newEvents =>
                                {
                                    //Make sure we don't try to insert into an occupied InsertedVersion
                                    newEvents.ForEach(refactoredEvent =>
                                    {
                                        refactoredEvent.RefactoringInformation.InsertedVersion = highestSeenVersion++;
                                    });

                                    refactoringEvents.Add(newEvents
                                                         .Select(@this => new EventDataRow(@event: @this.NewEvent,
                                                                                           @this.RefactoringInformation,
                                                                                           _typeMapper.GetId(@this.NewEvent.GetType()).GuidValue,
                                                                                           eventAsJson: _serializer.Serialize(@this.NewEvent)))
                                                         .ToList());

                                    updatedAggregates = updatedAggregatesBeforeMigrationOfThisAggregate + 1;
                                    newEventCount += newEvents.Count;
                                });

                            if(refactoringEvents.Count > 0)
                            {
                                refactoringEvents.ForEach(InsertSingleAggregateRefactoringEvents);

                                FixManualVersions(original, inMemoryMigratedHistory, refactoringEvents);

                                var loadedAggregateHistory = GetAggregateHistory(aggregateId);
                                AggregateHistoryValidator.ValidateHistory(aggregateId, loadedAggregateHistory);
                                AssertHistoriesAreIdentical(inMemoryMigratedHistory, loadedAggregateHistory);
                            }

                            migratedAggregates++;
                            succeeded = true;
                            transaction.Complete();
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

        void FixManualVersions(AggregateEventWithRefactoringInformation[] originalHistory, AggregateEvent[] newHistory, List<List<EventDataRow>> refactoringEvents)
        {
            var versionUpdates = new List<IEventStorePersistenceLayer.ManualVersionSpecification>();
            var replacedOrRemoved = originalHistory.Where(@this => newHistory.None(@event => @event.EventId == @this.Event.EventId)).ToList();
            versionUpdates.AddRange(replacedOrRemoved.Select(@this => new IEventStorePersistenceLayer.ManualVersionSpecification(@this.Event.EventId, -@this.RefactoringInformation.EffectiveVersion!.Value)));

            var replacedOrRemoved2 = refactoringEvents.SelectMany(@this =>@this).Where(@this => newHistory.None(@event => @event.EventId == @this.EventId));
            versionUpdates.AddRange(replacedOrRemoved2.Select(@this => new IEventStorePersistenceLayer.ManualVersionSpecification(@this.EventId, -@this.RefactoringInformation.EffectiveVersion!.Value)));

            versionUpdates.AddRange(newHistory.Select((@this , index) => new IEventStorePersistenceLayer.ManualVersionSpecification(@this.EventId, index + 1)));

            _persistenceLayer.UpdateEffectiveVersions(versionUpdates);
        }

        void InsertSingleAggregateRefactoringEvents(IReadOnlyList<EventDataRow> events)
        {
            // ReSharper disable PossibleInvalidOperationException
            var replacementGroup = events.Where(@event => @event.RefactoringInformation.Replaces.HasValue)
                                         .GroupBy(@event => @event.RefactoringInformation.Replaces!.Value)
                                         .SingleOrDefault();
            var insertBeforeGroup = events.Where(@event => @event.RefactoringInformation.InsertBefore.HasValue)
                                          .GroupBy(@event => @event.RefactoringInformation.InsertBefore!.Value)
                                          .SingleOrDefault();
            var insertAfterGroup = events.Where(@event => @event.RefactoringInformation.InsertAfter.HasValue)
                                         .GroupBy(@event => @event.RefactoringInformation.InsertAfter!.Value)
                                         .SingleOrDefault();
            // ReSharper restore PossibleInvalidOperationException

            Contract.Assert.That(Seq.Create(replacementGroup, insertBeforeGroup, insertAfterGroup).Where(@this => @this != null).Count() == 1,
                                 "Seq.Create(replacementGroup, insertBeforeGroup, insertAfterGroup).Where(@this => @this != null).Count() == 1");

            if (replacementGroup != null)
            {
                Contract.Assert.That(replacementGroup.All(@this => @this.RefactoringInformation.Replaces.HasValue && @this.RefactoringInformation.Replaces != Guid.Empty),
                                 "replacementGroup.All(@this => @this.Replaces.HasValue && @this.Replaces > 0)");
                ReplaceEvent(replacementGroup.Key, replacementGroup.ToArray());
            }
            else if (insertBeforeGroup != null)
            {
                Contract.Assert.That(insertBeforeGroup.All(@this => @this.RefactoringInformation.InsertBefore.HasValue && @this.RefactoringInformation.InsertBefore.Value != Guid.Empty),
                                 "insertBeforeGroup.All(@this => @this.InsertBefore.HasValue && @this.InsertBefore.Value > 0)");
                InsertBeforeEvent(insertBeforeGroup.Key, insertBeforeGroup.ToArray());
            }
            else if (insertAfterGroup != null)
            {
                Contract.Assert.That(insertAfterGroup.All(@this => @this.RefactoringInformation.InsertAfter.HasValue && @this.RefactoringInformation.InsertAfter.Value != Guid.Empty),
                                 "insertAfterGroup.All(@this => @this.InsertAfter.HasValue && @this.InsertAfter.Value > 0)");
                InsertAfterEvent(insertAfterGroup.Key, insertAfterGroup.ToArray());
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

        static void SetManualReadOrders(EventDataRow[] newEvents, SqlDecimal rangeStart, SqlDecimal rangeEnd)
        {
            var readOrderIncrement = (rangeEnd - rangeStart) / (newEvents.Length + 1);
            for (var index = 0; index < newEvents.Length; ++index)
            {
                //Urgent: Change this to another data type. https://github.com/mlidbom/Composable/issues/46
                var manualReadOrder = rangeStart + (index + 1) * readOrderIncrement;
                if (!(manualReadOrder.IsNull || (manualReadOrder.Precision == 38 && manualReadOrder.Scale == 17)))
                {
                    //urgent: unless we change the datatype this must not be removed.
                    //throw new ArgumentException($"$$$$$$$$$$$$$$$$$$$$$$$$$ Found decimal with precision: {manualReadOrder.Precision} and scale: {manualReadOrder.Scale}", nameof(manualReadOrder));
                }
                newEvents[index].RefactoringInformation.EffectiveOrder = manualReadOrder;
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
            public AggregateEventWithRefactoringInformation(AggregateEvent @event, AggregateEventRefactoringInformation refactoringInformation)
            {
                Event = @event;
                RefactoringInformation = refactoringInformation;
            }

            internal AggregateEvent Event { get; }
            internal AggregateEventRefactoringInformation RefactoringInformation { get; }
        }
    }
}