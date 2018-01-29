using System;
using Composable.DDD;

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

                public class EntityQuery<TResource> : Query<TResource> where TResource : IHasPersistentIdentity<Guid>
                {
                    public EntityQuery() {}
                    public EntityQuery(Guid entityId) => EntityId = entityId;
                    public EntityQuery<TResource> WithId(Guid id) => new EntityQuery<TResource>(id);
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

                public class RemoteEntityResourceQuery<TResource> : RemoteQuery<TResource> where TResource : IHasPersistentIdentity<Guid>
                {
                    public RemoteEntityResourceQuery() {}
                    public RemoteEntityResourceQuery(Guid entityId) => EntityId = entityId;
                    public RemoteEntityResourceQuery<TResource> WithId(Guid id) => new RemoteEntityResourceQuery<TResource>(id);
                    public Guid EntityId { get; private set; }
                }

                public class SelfGeneratingResourceQuery<TResource> : RemoteQuery<TResource>, ICreateMyOwnResultQuery<TResource> where TResource : new()
                {
                    SelfGeneratingResourceQuery() {}
                    public static readonly SelfGeneratingResourceQuery<TResource> Instance = new SelfGeneratingResourceQuery<TResource>();
                    public TResource CreateResult() => new TResource();
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
    }
}
