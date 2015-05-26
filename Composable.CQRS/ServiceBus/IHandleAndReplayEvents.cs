using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;

namespace Composable.ServiceBus
{
    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public  interface IHandleAndReplayEvents<TEvent>:IReplayEvents<TEvent>,IHandleMessages<TEvent> where TEvent:IMessage
    {
       
    }
   
}
