    using Composable.CQRS.EventSourcing;

namespace CQRS.Tests.CQRS.EventSourcing
{
    public class UserChangedPassword : AggregateRootEvent
    {
        public string Password { get; set; }
    }
}