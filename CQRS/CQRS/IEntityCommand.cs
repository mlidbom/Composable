using System;

namespace Composable.CQRS
{
    public interface IEntityCommand : IEntityCommand<Guid>
    {
        
    }

    public interface IEntityCommand<out TKeyType> 
    {
        TKeyType EntityId { get; }
    }
}