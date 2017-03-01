#region usings



#endregion

namespace Composable.Messaging.Commands
{
  using Composable.DomainEvents;

  using global::System.Collections.Generic;

  public class CommandResult
    {
        readonly IList<IDomainEvent> _events = new List<IDomainEvent>();

        public void RegisterEvent(IDomainEvent evt)
        {
            _events.Add(evt);
        }

        public IEnumerable<IDomainEvent> Events { get { return _events; } }
    }
}