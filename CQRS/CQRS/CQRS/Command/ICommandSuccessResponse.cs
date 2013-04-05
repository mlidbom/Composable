using Composable.DomainEvents;

namespace Composable.CQRS.Command
{
    public interface ICommandSuccessResponse : ICommandResponseMessage
    {
        IDomainEvent[] Events { get;}
    }
}