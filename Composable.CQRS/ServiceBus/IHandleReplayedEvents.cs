using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;

namespace Composable.ServiceBus
{
    public interface IHandleReplayedEvents<in TEvent> where TEvent:IMessage
    {
        void Handle(TEvent message);
    }
}
