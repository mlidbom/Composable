using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.CQRS.EventSourcing;

namespace Composable.Messaging
{
    ///<summary>
    /// <para>An <see cref="IEventSubscriber{TEvent}" /> that promises not to change any domain state. </para>
    /// <para></para>
    /// </summary>
    public interface IEventApplier<in TEvent> : IEventSubscriber<TEvent> where TEvent:IEvent
    {
    }
}
