using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.CQRS.EventSourcing;

namespace Composable.Messaging
{
    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public interface IHandleReplayedAndPublishedEvents<TEvent>: IEventApplier<TEvent>, IEventHandler<TEvent> where TEvent:IEvent
    {
       
    }
   
}
