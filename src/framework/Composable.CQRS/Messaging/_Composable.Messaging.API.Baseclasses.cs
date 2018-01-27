using System;
using Composable.DDD;

namespace Composable.Messaging
{
    public abstract class QueryResult {}

    public abstract class LocalQuery<TResult> : MessagingApi.IQuery<TResult> {}

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through its type and Id.</summary>
    public interface IEntityResource : IHasPersistentIdentity<Guid>
    {
    }

    public abstract class ExactlyOnceMessage : MessagingApi.IMessage, MessagingApi.Remote.ExactlyOnce.IExactlyOnceMessage
    {
        protected ExactlyOnceMessage() : this(Guid.NewGuid()) {}
        protected ExactlyOnceMessage(Guid id) => MessageId = id;

        public Guid MessageId { get; private set; } //Do not remove setter. Required for serialization
    }

    public abstract class EntityResource<TResource> : ExactlyOnceMessage, IEntityResource where TResource : EntityResource<TResource>
    {
        protected EntityResource() {}
        protected EntityResource(Guid id) => Id = id;
        public Guid Id { get; private set; }
    }

    public class Command : ValueObject<Command>, MessagingApi.Remote.ExactlyOnce.ICommand
    {
        public Guid MessageId { get; private set; }

        protected Command()
            : this(Guid.NewGuid()) {}

        Command(Guid id) => MessageId = id;
    }

    public class Command<TResult> : Command, MessagingApi.Remote.ExactlyOnce.ICommand<TResult> {}
}
