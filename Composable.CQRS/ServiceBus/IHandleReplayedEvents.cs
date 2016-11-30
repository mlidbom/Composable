using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.CQRS.EventSourcing;

namespace Composable.ServiceBus
{
    //Review:mlidbo: This should not constrain to IMessage, but rather to IEvent
    public interface IHandleReplayedEvents<in TEvent> where TEvent:IMessage
    {
        void Handle(TEvent message);
    }
}
