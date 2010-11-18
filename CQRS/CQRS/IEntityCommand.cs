using System;

namespace Composable.CQRS
{
    public interface IEntityCommand<T> 
    {
        Guid EntityId { get; }
    }
}