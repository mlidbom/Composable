using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS
{
    public abstract class TempAggregateRootWithTimeSourceSupport<TEntity, TBaseEvent> : AggregateRoot<TEntity, TBaseEvent>
        where TEntity : AggregateRoot<TEntity, TBaseEvent>
        where TBaseEvent : IAggregateRootEvent
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

        new protected void RaiseEvent(TBaseEvent theEvent)
        {
            theEvent.TimeStamp = TimeSource.LocalNow;
            base.RaiseEvent(theEvent);
        }
    }
}