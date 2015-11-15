using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS
{
    public abstract class TempAggregateRootWithTimeSourceSupport<TEntity, TBaseEventClass, TBaseEventInterface> : AggregateRootV2<TEntity, TBaseEventClass, TBaseEventInterface>
        where TEntity : TempAggregateRootWithTimeSourceSupport<TEntity, TBaseEventClass, TBaseEventInterface>
        where TBaseEventInterface : IAggregateRootEvent
        where TBaseEventClass : AggregateRootEvent, TBaseEventInterface
    {
        protected internal ITimeSource TimeSource { get; set; }

        [Obsolete("Only for infrastructure", true)]
        public TempAggregateRootWithTimeSourceSupport()
        {

        }

        protected TempAggregateRootWithTimeSourceSupport(ITimeSource timeSource)
        {
            TimeSource = timeSource;
        }

        new protected void RaiseEvent(TBaseEventClass @event)
        {
            @event.TimeStamp = TimeSource.LocalNow;
            base.RaiseEvent(@event);
        }
    }
}