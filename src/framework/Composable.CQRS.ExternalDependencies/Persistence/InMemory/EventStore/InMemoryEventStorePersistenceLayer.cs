using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading;
using Composable.Persistence.EventStore;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Persistence.InMemory.EventStore
{
    class InMemoryEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly OptimizedThreadShared<State> _state = new OptimizedThreadShared<State>(new State());
        readonly AggregateTransactionLockManager _aggregateTransactionLockManager = new AggregateTransactionLockManager();

        public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events) =>
            _state.WithExclusiveAccess(state =>
            {
                events.ForEach((@event, index) =>
                {
                    var insertionOrder = state.Events.Count + index;
                    @event.RefactoringInformation.EffectiveOrder ??= insertionOrder;
                    state.InsertionOrders.Add(@event.EventId, insertionOrder);
                });
                state.Events.AddRange(events);
            });

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) =>
            _state.WithExclusiveAccess(state => state
                                               .Events
                                               .OrderBy(@this => @this.RefactoringInformation.EffectiveOrder)
                                               .Where(@this => @this.AggregateId == aggregateId
                                                            && @this.RefactoringInformation.InsertedVersion > startAfterInsertedVersion
                                                            && @this.RefactoringInformation.EffectiveVersion > 0)
                                               .ToArray());

        public void UpdateEffectiveVersionAndEffectiveReadOrder(IReadOnlyList<IEventStorePersistenceLayer.ManualVersionSpecification> versions)
            => _state.WithExclusiveAccess(
                state =>
                {
                    foreach(var specification in versions)
                    {
                        var @event = state.Events.Single(@this => @this.EventId == specification.EventId).RefactoringInformation;
                        @event.EffectiveVersion = specification.EffectiveVersion;
                    }
                }
            );

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId)
            => _state.WithExclusiveAccess(state =>
            {
                var found = state.Events.Select((@event, index) => (index, @event)).Single(@this => @this.@event.EventId == eventId);

                var insertionOrder = found.index -1;
                var effectiveOrder = found.@event.RefactoringInformation.EffectiveOrder!.Value;
                var previousEventReadOrder = state.Events[found.index -1].RefactoringInformation.EffectiveOrder!.Value;


                var nextEventReadOrder = found.index < state.Events.Count -1
                                             ? state.Events[found.index + 1].RefactoringInformation.EffectiveOrder!.Value
                                             : found.index + 1;

                return new IEventStorePersistenceLayer.EventNeighborhood(
                    insertionOrder: insertionOrder,
                    effectiveReadOrder: effectiveOrder,
                    previousEventReadOrder: previousEventReadOrder,
                    nextEventReadOrder: nextEventReadOrder);
            });


        public IEnumerable<EventDataRow> StreamEvents(int batchSize)
            => _state.WithExclusiveAccess(state => state.Events.ToArray());

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
                        result.Add(new CreationEventRow(aggregateId:row.AggregateId, typeId: row.EventType));
                    }
                }

                return result;
            });

        public void DeleteAggregate(Guid aggregateId)
            => _state.WithExclusiveAccess(state => state.Events = state.Events.Where(row => row.AggregateId != aggregateId).ToList());

        public void SetupSchemaIfDatabaseUnInitialized()
        { /*Nothing to do for an in-memory storage*/
        }

        class State
        {
            public Dictionary<Guid, long> InsertionOrders = new Dictionary<Guid, long>();
            public List<EventDataRow> Events = new List<EventDataRow>();
        }

        class AggregateTransactionLockManager
        {
            readonly object _lock = new object();
            readonly Dictionary<Guid, SemaphoreSlim> _aggregateSemaphores = new Dictionary<Guid, SemaphoreSlim>();
        }
    }
}
