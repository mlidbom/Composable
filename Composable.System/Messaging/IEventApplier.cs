using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.CQRS.EventSourcing;

namespace Composable.Messaging
{
    public interface IEventApplier<in TEvent> : IEventSubscriber<TEvent> where TEvent:IEvent
    {
    }
}
