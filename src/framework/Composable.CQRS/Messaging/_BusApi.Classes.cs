using System;
using Composable.DDD;
using NetMQ.Sockets;
using Newtonsoft.Json;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public static partial class BusApi
    {
        public partial class StrictlyLocal
        {
            public static class Queries
            {
                public abstract class Query<TResult> : BusApi.StrictlyLocal.IQuery<TResult> {}

                public class EntityLink<TResult> : Query<TResult> where TResult : IHasPersistentIdentity<Guid>
                {
                    [JsonConstructor]public EntityLink(Guid entityId) => EntityId = entityId;
                    public Guid EntityId { get; private set; }
                }
            }

            public static class Commands
            {
                public abstract class Command : BusApi.StrictlyLocal.ICommand
                {
                }

                public abstract class Command<TResult> : BusApi.StrictlyLocal.ICommand<TResult>
                {
                }
            }
        }

        public static partial class Remotable
        {
            public static class Query
            {
                public abstract class RemoteQuery<TResult> : BusApi.Remotable.NonTransactional.IQuery<TResult> {}

                public class EntityLink<TResult> : RemoteQuery<TResult> where TResult : IHasPersistentIdentity<Guid>
                {
                    public EntityLink() {}
                    public EntityLink(Guid entityId) => EntityId = entityId;
                    public EntityLink<TResult> WithId(Guid id) => new EntityLink<TResult>(id);
                    public Guid EntityId { get; private set; }
                }

                ///<summary>Inherit to trivially easily implement <see cref="ICreateMyOwnResultQuery{TResult}"/> </summary>
                public abstract class FuncResultQuery<TResult> : RemoteQuery<TResult>, ICreateMyOwnResultQuery<TResult>
                {
                    readonly Func<TResult> _factory;
                    protected FuncResultQuery(Func<TResult> factory) => _factory = factory;
                    public TResult CreateResult() => _factory();
                }

                /// <summary>Implements <see cref="ICreateMyOwnResultQuery{TResult}"/> by calling the default constructor on <typeparamref name="TResult"/></summary>
                public class NewableResultLink<TResult> : RemoteQuery<TResult>, ICreateMyOwnResultQuery<TResult> where TResult : new()
                {
                    public TResult CreateResult() => new TResult();
                }
            }

            public static partial class AtMostOnce
            {
                public class Command : BusApi.Remotable.AtMostOnce.ICommand {}
                public class Command<TResult> : BusApi.Remotable.AtMostOnce.ICommand<TResult> {}
            }

            public static partial class NonTransactional
            {
            }

            public static partial class ExactlyOnce
            {
                public class Command : ValueObject<Command>, BusApi.Remotable.ExactlyOnce.ICommand
                {
                    public Guid MessageId { get; private set; }

                    protected Command()
                        : this(Guid.NewGuid()) {}

                    Command(Guid id) => MessageId = id;
                }
            }
        }

        public static class Response
        {
            public abstract class Entity<TResult> : IHasPersistentIdentity<Guid> where TResult : Entity<TResult>
            {
                protected Entity() {}
                protected Entity(Guid id) => Id = id;
                public Guid Id { get; private set; }
            }
        }
    }
}
