﻿using System;
using Composable.DDD;
using Composable.Messaging.Buses;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public partial class MessagingApi
    {
        public partial class Local
        {
            public class Queries
            {
                public abstract class Query<TResult> : MessagingApi.Local.IQuery<TResult> {}

                public class EntityQuery<TResource> : Query<TResource> where TResource : IHasPersistentIdentity<Guid>
                {
                    public EntityQuery() {}
                    public EntityQuery(Guid entityId) => EntityId = entityId;
                    public EntityQuery<TResource> WithId(Guid id) => new EntityQuery<TResource>(id);
                    public Guid EntityId { get; private set; }
                }
            }

            public class Commands
            {
                public abstract class Command : MessagingApi.Local.ICommand
                {
                }

                public abstract class Command<TResult> : MessagingApi.Local.ICommand<TResult>
                {
                }
            }
        }

        public partial class Remote
        {
            public class Query
            {
                public abstract class RemoteQuery<TResult> : MessagingApi.Remote.NonTransactional.IQuery<TResult> {}

                public class RemoteEntityResourceQuery<TResource> : RemoteQuery<TResource> where TResource : IHasPersistentIdentity<Guid>
                {
                    public RemoteEntityResourceQuery() {}
                    public RemoteEntityResourceQuery(Guid entityId) => EntityId = entityId;
                    public RemoteEntityResourceQuery<TResource> WithId(Guid id) => new RemoteEntityResourceQuery<TResource>(id);
                    public Guid EntityId { get; private set; }
                }

                public class SelfGeneratingResourceQuery<TResource> : ICreateMyOwnResultQuery<TResource> where TResource : new()
                {
                    SelfGeneratingResourceQuery() {}
                    public static readonly SelfGeneratingResourceQuery<TResource> Instance = new SelfGeneratingResourceQuery<TResource>();
                    public TResource CreateResult() => new TResource();
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
