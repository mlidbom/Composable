using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.CQRS.EventSourcing;

namespace Composable.ServiceBus
{
    public interface IHandleReplayedEvents<in TEvent> where TEvent:IEvent
    {
        void Handle(TEvent message);
    }
}
