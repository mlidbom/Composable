using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventHandling;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing
{
    public interface IReadOnlyEntityCollection<TEntity, in TEntityId>
    {
        IReadOnlyList<TEntity> InCreationOrder { get; }
        bool TryGet(TEntityId id, out TEntity component);
        bool Exists(TEntityId id);
        TEntity Get(TEntityId id);
        TEntity this[TEntityId id] { get; }
    }



    public interface IReadOnlyEntityCollection<TEntity> : IReadOnlyEntityCollection<TEntity, Guid>
    {
        
    }


    public interface IEntityCollectionManager<TEntity, in TEntityId, TEventClass, in TEntityCreationInterface>
    {
        IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
        TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
            where TCreationEvent : TEventClass, TEntityCreationInterface;
    }

    public interface IEntityCollectionManager<TEntity, TEventClass, TEntityCreationInterface> : IEntityCollectionManager<TEntity, Guid, TEventClass, TEntityCreationInterface>
    {

    }    
}
