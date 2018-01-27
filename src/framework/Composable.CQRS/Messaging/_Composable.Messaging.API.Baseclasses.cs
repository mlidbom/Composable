using System;
using Composable.DDD;

namespace Composable.Messaging
{
    public abstract class QueryResult {}

    public abstract class RemoteQuery<TResult> : IQuery<TResult> {}

    public abstract class LocalQuery<TResult> : IQuery<TResult> {}

    public class RemoteEntityResourceQuery<TResource> : RemoteQuery<TResource> where TResource : IHasPersistentIdentity<Guid>
    {
        public RemoteEntityResourceQuery() {}
        public RemoteEntityResourceQuery(Guid entityId) => EntityId = entityId;
        public RemoteEntityResourceQuery<TResource> WithId(Guid id) => new RemoteEntityResourceQuery<TResource>(id);
        public Guid EntityId { get; private set; }
    }

    ///<summary>Represent an entity within the domain of the current API that is uniquely identifiable through its type and Id.</summary>
    public interface IEntityResource<TResource> : IHasPersistentIdentity<Guid>
    {
    }

    public abstract class ExactlyOnceMessage : IMessage, IExactlyOnceMessage
    {
        protected ExactlyOnceMessage() : this(Guid.NewGuid()) {}
        protected ExactlyOnceMessage(Guid id) => MessageId = id;

        public Guid MessageId { get; private set; } //Do not remove setter. Required for serialization
    }

    public abstract class EntityResource<TResource> : ExactlyOnceMessage, IEntityResource<TResource> where TResource : EntityResource<TResource>
    {
        protected EntityResource() {}
        protected EntityResource(Guid id) => Id = id;
        public Guid Id { get; private set; }
    }

    public class ExactlyOnceCommand : ValueObject<ExactlyOnceCommand>, IExactlyOnceCommand
    {
        public Guid MessageId { get; private set; }

        protected ExactlyOnceCommand()
            : this(Guid.NewGuid()) {}

        ExactlyOnceCommand(Guid id) => MessageId = id;
    }

    public class ExactlyOnceCommand<TResult> : ExactlyOnceCommand, IExactlyOnceCommand<TResult> {}
}
