using System;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain.Events.Implementation
{
    public class AccountEvent : AggregateRootEvent, IAccountEvent
    {
        protected AccountEvent() {}
        protected AccountEvent(Guid aggregateRootId) : base(aggregateRootId) {}
    }
}