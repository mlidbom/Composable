#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.GenericAbstractions.Time;
using Composable.Logging.Log4Net;
using Composable.ServiceBus;
using Composable.StuffThatDoesNotBelongHere;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;

#endregion

namespace Composable.CQRS.EventSourcing
{
    //Review:mlidbo: Detect and warn about using the session within multiple transactions. That it is likely to result in optimistic concurrency exceptions.
    public class EventStoreSession :
        IEventStoreReader,
        IEventStoreSession,
        IUnitOfWorkParticipantWhoseCommitMayTriggerChangesInOtherParticipantsMustImplementIdemponentCommit
    {
        private readonly IServiceBus _bus;
        private readonly IEventStore _store;
        private static ILog Log = LogManager.GetLogger(typeof(EventStoreSession));
        private readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();
        private readonly HashSet<Guid> _publishedEvents = new HashSet<Guid>();
        private readonly ISingleContextUseGuard _usageGuard;
        private readonly List<Guid> _pendingDeletes = new List<Guid>();
        protected internal ITimeSource TimeSource { get; set; }


        [Obsolete("The sessions now absolutely require a time source. This is only here for binary backwards compatability and will be removed SOON.", error:true)]
        public EventStoreSession(IServiceBus bus, IEventStore store, ISingleContextUseGuard usageGuard) : this(bus, store, usageGuard, DateTimeNowTimeSource.Instance)
        {
            this.Log().Warn($"Using obsolete method in {nameof(EventStoreSession)}. This might cause inconsistencies in date time values. please upgrade Composable.Cqrs ASAP. This method will be removed SOON");
        }

        public EventStoreSession(IServiceBus bus, IEventStore store, ISingleContextUseGuard usageGuard, ITimeSource timeSource)
        {
            Contract.Requires(bus != null);
            Contract.Requires(store != null);
            Contract.Requires(usageGuard != null);
            Contract.Requires(timeSource != null);

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
                Log.DebugFormat("{0} ignored call to SaveChanges since participating in a unit of work", _id);
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

        private IUnitOfWork _unitOfWork;
        private readonly Guid _id = Guid.NewGuid();

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
            IList<IAggregateRootEvent> history = _store.GetAggregateHistory(aggregateId).ToList();

            var version = 1;
            foreach (var aggregateRootEvent in history)
            {
                if (aggregateRootEvent.AggregateRootVersion != version++)
                {
                    throw new InvalidHistoryException();
                }
            }
            return history;
        }

        private bool DoTryGet<TAggregate>(Guid aggregateId, out TAggregate aggregate) where TAggregate : IEventStored
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

            var history = GetHistory(aggregateId).ToList();
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

        private TAggregate CreateInstance<TAggregate>() where TAggregate : IEventStored
        {
            var aggregate = (TAggregate)Activator.CreateInstance(typeof(TAggregate), nonPublic: true);
            aggregate.SetTimeSource(TimeSource);
            return aggregate;
        }        

        private void PublishUnpublishedEvents(IEnumerable<IAggregateRootEvent> events)
        {
            var unpublishedEvents = events.Where(e => !_publishedEvents.Contains(e.EventId))
                                          .ToList();
            _publishedEvents.AddRange(unpublishedEvents.Select(e => e.EventId));
            unpublishedEvents.ForEach(_bus.Publish);
        }

        private bool InternalSaveChanges()
        {
            Log.DebugFormat("{0} saving changes with {1} changes from transaction within unit of work {2}", _id, _idMap.Count, _unitOfWork ?? (object)"null");

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

    internal class ParticipantAccessedByWrongUnitOfWork : Exception { }
}