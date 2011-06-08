using Composable.CQRS.EventSourcing;

namespace CQRS.Tests.CQRS.EventSourcing
{
    public class UserChangedEmail : AggregateRootEvent
    {
        public UserChangedEmail(string email)
        {
            Email = email;
        }
        public string Email { get; private set; }
    }
}