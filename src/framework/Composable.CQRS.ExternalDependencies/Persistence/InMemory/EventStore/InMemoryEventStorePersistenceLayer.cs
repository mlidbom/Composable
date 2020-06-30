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
            _state.WithExclusiveAccess(state =>
            {
                foreach(var row in events)
                {
                    row.InsertionOrder = ++state.InsertionOrder;
                    row.RefactoringInformation.EffectiveVersion = row.RefactoringInformation.ManualVersion;
                    state.Events.Add(row);
                }
            });

        public void InsertAfterEvent(Guid eventId, EventDataRow[] insertAfterGroup) { throw new NotImplementedException(); }
        public void InsertBeforeEvent(Guid eventId, EventDataRow[] insertBeforeGroup) { throw new NotImplementedException(); }
        public void ReplaceEvent(Guid eventId, EventDataRow[] replacementGroup) { throw new NotImplementedException(); }
        public void FixManualVersions(Guid aggregateId) { throw new NotImplementedException(); }

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) =>
            _state.WithExclusiveAccess(state => state
                                               .Events
                                               .Where(@this => @this.AggregateId == aggregateId && @this.RefactoringInformation.InsertedVersion > startAfterInsertedVersion)
                                               .ToArray());

        public IEnumerable<EventDataRow> StreamEvents(int batchSize)
            => _state.WithExclusiveAccess(state => state.Events.ToArray());

        public IEnumerable<Guid> ListAggregateIdsInCreationOrder(Type? eventBaseType = null)
            => _state.WithExclusiveAccess(state =>
            {
                var found = new HashSet<Guid>();
                var result = new List<Guid>();
                foreach(var row in state.Events)
                {
                    if(!found.Contains(row.AggregateId))
                    {
                        found.Add(row.AggregateId);
                        result.Add(row.AggregateId);
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
