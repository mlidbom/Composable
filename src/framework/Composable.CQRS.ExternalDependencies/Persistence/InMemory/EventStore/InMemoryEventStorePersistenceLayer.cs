using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Persistence.EventStore;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using Composable.SystemExtensions.TransactionsCE;

namespace Composable.Persistence.InMemory.EventStore
{
    class InMemoryEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly OptimizedThreadShared<State> _state = new OptimizedThreadShared<State>(new State());
        readonly AggregateTransactionLockManager _aggregateTransactionLockManager = new AggregateTransactionLockManager();

        public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events) =>
            _aggregateTransactionLockManager.WithExclusiveAccess(
                events.First().AggregateId,
                () => _state.WithExclusiveAccess(state =>
                {
                    events.ForEach((@event, index) =>
                    {
                        var insertionOrder = state.Events.Count + index + 1;
                        @event.RefactoringInformation.EffectiveOrder ??= insertionOrder;
                    });
                    state.Events.AddRange(events);
                }));

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
            => _aggregateTransactionLockManager.WithExclusiveAccess(
                aggregateId,
                takeWriteLock,
                () => _state.WithExclusiveAccess(
                    state => state
                            .Events
                            .OrderBy(@this => @this.RefactoringInformation.EffectiveOrder)
                            .Where(@this => @this.AggregateId == aggregateId
                                         && @this.RefactoringInformation.InsertedVersion > startAfterInsertedVersion
                                         && @this.RefactoringInformation.EffectiveVersion > 0)
                            .ToArray()));

        public void UpdateEffectiveVersions(IReadOnlyList<IEventStorePersistenceLayer.ManualVersionSpecification> versions)
            => _aggregateTransactionLockManager.WithExclusiveAccess(
                _state.WithExclusiveAccess(state => state.Events.Single(@event => @event.EventId == versions.First().EventId)).AggregateId,
                () => _state.WithExclusiveAccess(
                    state =>
                    {
                        foreach(var specification in versions)
                        {
                            var (@event, index) = state.Events
                                                       .Select((eventRow, innerIndex) => (eventRow, innerIndex))
                                                       .Single(@this => @this.eventRow.EventId == specification.EventId);

                            state.Events[index] = new EventDataRow(@event.EventType,
                                                                   @event.EventJson,
                                                                   @event.EventId,
                                                                   specification.EffectiveVersion,
                                                                   @event.AggregateId,
                                                                   @event.UtcTimeStamp,
                                                                   new AggregateEventRefactoringInformation()
                                                                   {
                                                                       EffectiveVersion = specification.EffectiveVersion,
                                                                       EffectiveOrder = @event.RefactoringInformation.EffectiveOrder,
                                                                       InsertedVersion = @event.RefactoringInformation.InsertedVersion,
                                                                       Replaces = @event.RefactoringInformation.Replaces,
                                                                       InsertBefore = @event.RefactoringInformation.InsertBefore,
                                                                       InsertAfter = @event.RefactoringInformation.InsertAfter
                                                                   });
                        }
                    }
                ));

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId)
            => _aggregateTransactionLockManager.WithExclusiveAccess(
                _state.WithExclusiveAccess(state => state.Events.Single(@event => @event.EventId == eventId)).AggregateId,
                () => _state.WithExclusiveAccess(state =>
                {
                    var found = state.Events.Single(@this => @this.EventId == eventId);

                    var effectiveOrder = found.RefactoringInformation.EffectiveOrder!.Value;
                    var previousEventReadOrder = state.Events
                                                      .Where(@this => (@this.RefactoringInformation.EffectiveOrder!.Value < effectiveOrder).Value)
                                                      .OrderByDescending(@this => @this.RefactoringInformation.EffectiveOrder)
                                                      .First()
                                                      .RefactoringInformation.EffectiveOrder!.Value;

                    var nextEvent = state.Events
                                         .Where(@this => (@this.RefactoringInformation.EffectiveOrder!.Value > effectiveOrder).Value)
                                         .OrderBy(@this => @this.RefactoringInformation.EffectiveOrder)
                                         .FirstOrDefault();

                    var nextEventReadOrder = nextEvent?.RefactoringInformation.EffectiveOrder ?? effectiveOrder + 1;

                    return new IEventStorePersistenceLayer.EventNeighborhood(effectiveReadOrder: effectiveOrder,
                                                                             previousEventReadOrder: previousEventReadOrder,
                                                                             nextEventReadOrder: nextEventReadOrder);
                }));

        public IEnumerable<EventDataRow> StreamEvents(int batchSize)
            => _state.WithExclusiveAccess(state => state.Events
                                                        .OrderBy(@event => @event.RefactoringInformation.EffectiveOrder)
                                                        .Where(@event => @event.RefactoringInformation.EffectiveVersion > 0)
                                                        .ToArray());

        public IReadOnlyList<CreationEventRow> ListAggregateIdsInCreationOrder()
            => _state.WithExclusiveAccess(state =>
            {
                var found = new HashSet<Guid>();
                var result = new List<CreationEventRow>();
                foreach(var row in state.Events.Where(@event => @event.AggregateVersion == 1))
                {
                    if(!found.Contains(row.AggregateId))
                    {
                        found.Add(row.AggregateId);
                        result.Add(new CreationEventRow(aggregateId: row.AggregateId, typeId: row.EventType));
                    }
                }

                return result;
            });

        public void DeleteAggregate(Guid aggregateId)
            => _aggregateTransactionLockManager.WithExclusiveAccess(
                aggregateId,
                () => _state.WithExclusiveAccess(state => state.Events = state.Events.Where(row => row.AggregateId != aggregateId).ToList()));

        public void SetupSchemaIfDatabaseUnInitialized()
        { /*Nothing to do for an in-memory storage*/
        }

        class State
        {
            public List<EventDataRow> Events = new List<EventDataRow>();
        }

        class AggregateTransactionLockManager
        {
            readonly Dictionary<Guid, TransactionWideLock> _aggregateGuards = new Dictionary<Guid, TransactionWideLock>();

            public TResult WithExclusiveAccess<TResult>(Guid aggregateId, Func<TResult> func) => WithExclusiveAccess(aggregateId, true, func);
            public TResult WithExclusiveAccess<TResult>(Guid aggregateId, bool takeWriteLock, Func<TResult> func)
            {
                if(Transaction.Current != null)
                {
                    var @lock = _aggregateGuards.GetOrAdd(aggregateId, () => new TransactionWideLock());
                    @lock.AwaitAccess(takeWriteLock);
                }

                return func();
            }

            public void WithExclusiveAccess(Guid aggregateId, Action action) => WithExclusiveAccess(aggregateId, true, () =>
            {
                action();
                return 1;
            });

            class TransactionWideLock
            {
                public TransactionWideLock() => Guard = ResourceGuard.WithTimeout(1.Minutes());

                public void AwaitAccess(bool takeWriteLock)
                {
                    if(OwningTransactionLocalId == string.Empty && !takeWriteLock)
                    {
                        return;
                    }

                    var currentTransactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
                    if(currentTransactionId != OwningTransactionLocalId)
                    {
                        var @lock = Guard.AwaitUpdateLock();
                        Transaction.Current.OnCompleted(() =>
                        {
                            HasWriteLock = false;
                            OwningTransactionLocalId = string.Empty;
                            @lock.Dispose();
                        });
                        OwningTransactionLocalId = currentTransactionId;
                    }
                }

                bool HasWriteLock { get; set; }
                string OwningTransactionLocalId { get; set; } = string.Empty;
                IResourceGuard Guard { get; }
            }
        }
    }
}
