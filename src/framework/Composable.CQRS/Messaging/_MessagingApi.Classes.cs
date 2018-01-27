using System;
using Composable.DDD;
// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public partial class MessagingApi
    {
        public partial class Local
        {
        }

        public partial class Remote
        {

            public class Query
            {
                public abstract class RemoteQuery<TResult> : MessagingApi.IQuery<TResult> {}

                public class RemoteEntityResourceQuery<TResource> : RemoteQuery<TResource> where TResource : IHasPersistentIdentity<Guid>
                {
                    public RemoteEntityResourceQuery() {}
                    public RemoteEntityResourceQuery(Guid entityId) => EntityId = entityId;
                    public RemoteEntityResourceQuery<TResource> WithId(Guid id) => new RemoteEntityResourceQuery<TResource>(id);
                    public Guid EntityId { get; private set; }
                }
            }

            public partial class NonTransactional
            {
            }

            public partial class ExactlyOnce
            {
                public abstract class Message : MessagingApi.IMessage, MessagingApi.Remote.ExactlyOnce.IExactlyOnceMessage
                {
                    protected Message() : this(Guid.NewGuid()) {}
                    protected Message(Guid id) => MessageId = id;

                    public Guid MessageId { get; private set; } //Do not remove setter. Required for serialization
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
        }
    }
}
