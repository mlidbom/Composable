#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;

#endregion

namespace Composable.CQRS.CQRS.EventSourcing
{
    //Review:mlidbo: Detect and warn about using the session within multiple transactions. That it is likely to result in optimistic concurrency exceptions.
    public class EventStoreSession :
        IEventStoreReader,
        IEventStoreSession,
        IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit
    {
        readonly IServiceBus _bus;
        readonly IEventStore _store;
        static ILog _log = LogManager.GetLogger(typeof(EventStoreSession));
        readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();
        readonly HashSet<Guid> _publishedEvents = new HashSet<Guid>();
        readonly ISingleContextUseGuard _usageGuard;
        readonly List<Guid> _pendingDeletes = new List<Guid>();
        IUtcTimeTimeSource TimeSource { get; set; }


        public EventStoreSession(IServiceBus bus, IEventStore store, ISingleContextUseGuard usageGuard, IUtcTimeTimeSource timeSource)
        {
            Contract.Argument(() => bus, () => store, () => usageGuard, () => timeSource)
                        .NotNull();

            _usageGuard = usageGuard;
            _bus = bus;
            _store = store;
            TimeSource = timeSource ?? DateTimeNowTimeSource.Instance;
        }

        public TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            TAggregate result;
            if (!DoTryGet(aggregateId, out result))
            {
                throw new AggregateRootNotFoundException(aggregateId);
            }
            return result;
        }

        public bool TryGet<TAggregate>(Guid aggregateId, out TAggregate aggregate) where TAggregate : IEventStored
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            return DoTryGet(aggregateId, out aggregate);
        }

        public TAggregate LoadSpecificVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored
        {
            Contract.Assert(version > 0);

            _usageGuard.AssertNoContextChangeOccurred(this);
            var aggregate = CreateInstance<TAggregate>();
            var history = GetHistory(aggregateId);
            if (history.None())
            {
                throw new AggregateRootNotFoundException(aggregateId);
            }
            aggregate.LoadFromHistory(history.Where(e => e.AggregateRootVersion <= version));
            return aggregate;
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            var changes = aggregate.GetChanges().ToList();
            if (aggregate.Version > 0 && changes.None() || changes.Any() && changes.Min(e => e.AggregateRootVersion) > 1)
            {
                throw new AttemptToSaveAlreadyPersistedAggregateException(aggregate);
            }
            if (aggregate.Version == 0 && changes.None())
            {
                throw new AttemptToSaveEmptyAggregate(aggregate);
            }
            _idMap.Add(aggregate.Id, aggregate);
        }

        public void SaveChanges()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if (_unitOfWork == null)
            {
                InternalSaveChanges();
            }
            else
            {
                var newEvents = _idMap.SelectMany(p => p.Value.GetChanges()).ToList();
                PublishUnpublishedEvents(newEvents);
                _log.DebugFormat("{0} ignored call to SaveChanges since participating in a unit of work", _id);
            }
        }

        public void Delete(Guid aggregateId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _pendingDeletes.Add(aggregateId);
        }

        public void Dispose()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _store.Dispose();
        }


        public override string ToString()
        {
            return "{0}: {1}".FormatWith(_id, GetType().FullName);
        }


        #region Implementation of IUnitOfWorkParticipant

        IUnitOfWork _unitOfWork;
        readonly Guid _id = Guid.NewGuid();

        IUnitOfWork IUnitOfWorkParticipant.UnitOfWork { get { return _unitOfWork; } }
        Guid IUnitOfWorkParticipant.Id { get { return _id; } }

        void IUnitOfWorkParticipant.Join(IUnitOfWork unit)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if (_unitOfWork != null)
            {
                throw new ReuseOfEventStoreSessionException(_unitOfWork, unit);
            }
            _unitOfWork = unit;
        }

        void IUnitOfWorkParticipant.Commit(IUnitOfWork unit)
        {
            if (unit != _unitOfWork)
            {
                throw new ParticipantAccessedByWrongUnitOfWork();
            }
            _usageGuard.AssertNoContextChangeOccurred(this);
            ((IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit)this).CommitAndReportIfCommitMayHaveCausedChangesInOtherParticipantsExpectAnotherCommitSoDoNotLeaveUnitOfWork();
            _unitOfWork = null;
        }

        void IUnitOfWorkParticipant.Rollback(IUnitOfWork unit)
        {
            if (unit != _unitOfWork)
            {
                throw new ParticipantAccessedByWrongUnitOfWork();
            }
            _usageGuard.AssertNoContextChangeOccurred(this);
            _unitOfWork = null;
        }

        bool IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit.CommitAndReportIfCommitMayHaveCausedChangesInOtherParticipantsExpectAnotherCommitSoDoNotLeaveUnitOfWork()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            return InternalSaveChanges();
        }

        #endregion

        public IEnumerable<IAggregateRootEvent> GetHistory(Guid aggregateId)
        {
            return GetHistoryInternal(aggregateId, takeWriteLock:false);
        }

        IEnumerable<IAggregateRootEvent> GetHistoryInternal(Guid aggregateId, bool takeWriteLock)
        {
            IList<IAggregateRootEvent> history = (takeWriteLock
                                                     ? _store.GetAggregateHistoryForUpdate(aggregateId)
                                                     : _store.GetAggregateHistory(aggregateId)).ToList();

            var version = 1;
            foreach (var aggregateRootEvent in history)
            {
                if (aggregateRootEvent.AggregateRootVersion != version++)
                {
                    throw new InvalidHistoryException(aggregateId);
                }
            }
            return history;
        }

        bool DoTryGet<TAggregate>(Guid aggregateId, out TAggregate aggregate) where TAggregate : IEventStored
        {
            if (_pendingDeletes.Contains(aggregateId))
            {
                aggregate = default(TAggregate);
                return false;
            }

            IEventStored es;
            if (_idMap.TryGetValue(aggregateId, out es))
            {
                aggregate = (TAggregate)es;
                return true;
            }

            var history = GetHistoryInternal(aggregateId, takeWriteLock: true).ToList();
            if (history.Any())
            {
                aggregate = CreateInstance<TAggregate>();
                aggregate.LoadFromHistory(history);
                _idMap.Add(aggregateId, aggregate);
                return true;
            }
            else
            {
                aggregate = default(TAggregate);
                return false;
            }
        }

        TAggregate CreateInstance<TAggregate>() where TAggregate : IEventStored
        {
            var aggregate = (TAggregate)Activator.CreateInstance(typeof(TAggregate), nonPublic: true);
            aggregate.SetTimeSource(TimeSource);
            return aggregate;
        }

        void PublishUnpublishedEvents(IEnumerable<IAggregateRootEvent> events)
        {
            var unpublishedEvents = events.Where(e => !_publishedEvents.Contains(e.EventId))
                                          .ToList();
            _publishedEvents.AddRange(unpublishedEvents.Select(e => e.EventId));
            unpublishedEvents.ForEach(_bus.Publish);
        }

        bool InternalSaveChanges()
        {
            _log.DebugFormat("{0} saving changes with {1} changes from transaction within unit of work {2}", _id, _idMap.Count, _unitOfWork ?? (object)"null");

            var aggregates = _idMap.Select(p => p.Value).ToList();

            var newEvents = aggregates.SelectMany(a => a.GetChanges()).ToList();
            aggregates.ForEach(a => a.AcceptChanges());
            _store.SaveEvents(newEvents);

            PublishUnpublishedEvents(newEvents);

            bool result = newEvents.Any() || _pendingDeletes.Any();

            foreach (var toDelete in _pendingDeletes)
            {
                _store.DeleteEvents(toDelete);
                _idMap.Remove(toDelete);
            }
            _pendingDeletes.Clear();

            return result;
        }
    }

    class ParticipantAccessedByWrongUnitOfWork : Exception { }
}