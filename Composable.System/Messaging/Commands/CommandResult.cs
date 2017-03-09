using System.Collections.Generic;
using Composable.DomainEvents;

namespace Composable.Messaging.Commands
{
    public class CommandResult
    {
        readonly IList<IDomainEvent> _events = new List<IDomainEvent>();

        public void RegisterEvent(IDomainEvent evt) { _events.Add(evt); }

        public IEnumerable<IDomainEvent> Events { get { return _events; } }
    }
}
