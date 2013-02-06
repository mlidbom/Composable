using System;

namespace Composable.CQRS.UI.Event
{
    public interface IUIEvent
    {
        global::System.Guid Id { get; }
        Guid AggregateRootId { get; set; }
        DateTime TimeStamp { get; set; }
    }
}