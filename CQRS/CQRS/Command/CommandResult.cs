#region usings

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Composable.DomainEvents;

#endregion

namespace Composable.CQRS
{
    public class CommandResult
    {
        private readonly IList<IDomainEvent> _events = new List<IDomainEvent>();

        public void RegisterEvent(IDomainEvent evt)
        {
            _events.Add(evt);
        }

        public IEnumerable<IDomainEvent> Events { get { return _events; } }
    }
}