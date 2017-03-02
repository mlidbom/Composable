using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.CQRS.EventSourcing;

namespace Composable.Messaging
{
    ///<summary>
    /// <para>An <see cref="IEventListener{TEvent}" /> that promises not to change any domain state. </para>
    /// <para></para>
    /// </summary>
    public interface IEventApplier<in TEvent> : IEventListener<TEvent> where TEvent:IEvent
    {
    }
}
