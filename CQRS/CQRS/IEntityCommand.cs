using System;

namespace Composable.CQRS
{
    public interface IEntityCommand : IDomainCommand
    {
        object EntityId { get; }
    }    
}