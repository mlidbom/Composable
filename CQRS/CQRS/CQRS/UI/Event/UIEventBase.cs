using System;

namespace Composable.CQRS.UI.Event
{
    public class UIEventBase : IUIEvent
    {
        public Guid Id { get; set; }
        public Guid AggregateRootId { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}