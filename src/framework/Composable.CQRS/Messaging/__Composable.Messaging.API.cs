using System;
// ReSharper disable UnusedTypeParameter

namespace Composable.Messaging
{
    ///<summary>An object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
    public interface IMessage {}

    ///<summary>Informs the receiver that something has happened.</summary>
    public interface IEvent { }

    /// <summary>Instructs the recevier to perform an action.</summary>
    public interface ICommand { }

    public interface INonTransactionalAtMostOnceDeliveryMessage : IMessage{}

    public interface ITransactionalExactlyOnceDeliveryMessage : IMessage
    {
        Guid MessageId { get; }
    }

    public interface INonTransactionalAtMostOnceDeliveryEvent : IEvent, INonTransactionalAtMostOnceDeliveryMessage { }
    public interface INonTransactionalAtMostOnceDeliveryCommand : ICommand, INonTransactionalAtMostOnceDeliveryMessage { }

    public interface IQuery : INonTransactionalAtMostOnceDeliveryMessage { }

    ///<summary>An <see cref="IMessage"/> that instructs the receiver to return a resource based upon the data in the query.</summary>
    public interface IQuery<TResult> : IQuery { }


    public interface ITransactionalExactlyOnceDeliveryEvent : IEvent, ITransactionalExactlyOnceDeliveryMessage { }
    public interface ITransactionalExactlyOnceDeliveryCommand : ITransactionalExactlyOnceDeliveryMessage { }
    public interface ITransactionalExactlyOnceDeliveryCommand<TResult> : ITransactionalExactlyOnceDeliveryCommand { }


    public interface IEntityQuery<TEntity> : IQuery<TEntity>
    {
        Guid Id { get; }
    }

    public class TryGetEntityQuery<TEntity> : IEntityQuery<TryGetEntityQueryResult<TEntity>>
    {
        public Guid Id { get; }
        public static TryGetEntityQuery<TEntity> WithEntityId(Guid entityId) => new TryGetEntityQuery<TEntity>(entityId);
        TryGetEntityQuery(Guid entityId) { Id = entityId; }
    }

    public class TryGetEntityQueryResult<TEntity>
    {
        public TryGetEntityQueryResult(bool foundEntity, TEntity result)
        {
            FoundEntity = foundEntity;
            Result = result;
        }

        public bool FoundEntity { get; }
        public TEntity Result { get; }

        public static TryGetEntityQueryResult<TEntity> SuccessFul(TEntity result) => new TryGetEntityQueryResult<TEntity>(true, result);
        public static TryGetEntityQueryResult<TEntity>Failed() => new TryGetEntityQueryResult<TEntity>(false, default);
    }
}
