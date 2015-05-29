using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;

namespace Composable.ServiceBus
{
    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public interface IHandleReplayedAndPublishedEvents<TEvent>:IHandleReplayedEvents<TEvent>,IHandleMessages<TEvent> where TEvent:IMessage
    {
       
    }
   
}
