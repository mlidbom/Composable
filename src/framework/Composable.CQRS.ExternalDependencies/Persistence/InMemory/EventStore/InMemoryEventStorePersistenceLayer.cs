using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Persistence.EventStore;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Persistence.InMemory.EventStore
{
    class InMemoryEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly OptimizedThreadShared<State> _state = new OptimizedThreadShared<State>(new State());
        readonly AggregateTransactionLockManager _aggregateTransactionLockManager = new AggregateTransactionLockManager();

        public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events) =>
            _state.WithExclusiveAccess(state => state.Events.AddRange(events));

        public void UpdateEffectiveVersionAndEffectiveReadOrder(IReadOnlyList<IEventStorePersistenceLayer.ManualVersionSpecification> versions) { throw new NotImplementedException(); }

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId) => throw new NotImplementedException();

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) =>
            _state.WithExclusiveAccess(state => state
                                               .Events
                                               .Where(@this => @this.AggregateId == aggregateId
                                                            && @this.RefactoringInformation.InsertedVersion > startAfterInsertedVersion
                                                            && @this.RefactoringInformation.EffectiveVersion > 0)
                                               .ToArray());

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
            public long InsertionOrder;
            public List<EventDataRow> Events = new List<EventDataRow>();
        }

        class AggregateTransactionLockManager
        {
            readonly object _lock = new object();
            readonly Dictionary<Guid, SemaphoreSlim> _aggregateSemaphores = new Dictionary<Guid, SemaphoreSlim>();
        }
    }
}
