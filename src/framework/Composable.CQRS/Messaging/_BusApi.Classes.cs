using System;
using Composable.DDD;
using Composable.Messaging.Buses;
using Composable.Refactoring.Naming;
using Composable.System.Reflection;
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
                public abstract class Query<TResult> : StrictlyLocal.IQuery<TResult> {}

                public class EntityLink<TResult> : StrictlyLocal.Queries.Query<TResult> where TResult : IHasPersistentIdentity<Guid>
                {
                    [JsonConstructor]public EntityLink(Guid entityId) => EntityId = entityId;
                    public Guid EntityId { get; private set; }
                }
            }

            public static class Commands
            {
                public abstract class Command : StrictlyLocal.ICommand
                {
                }

                public abstract class Command<TResult> : StrictlyLocal.ICommand<TResult>
                {
                }
            }
        }

        public static partial class Remotable
        {
            public static partial class AtMostOnce
            {
                public class Command : Remotable.AtMostOnce.ICommand {}
                public class Command<TResult> : Remotable.AtMostOnce.ICommand<TResult> {}
            }

            public static partial class NonTransactional
            {
                public static class Queries
                {
                    public abstract class Query<TResult> : Remotable.NonTransactional.IQuery<TResult> {}

                    public class EntityLink<TResult> : Remotable.NonTransactional.Queries.Query<TResult> where TResult : IHasPersistentIdentity<Guid>
                    {
                        public EntityLink() {}
                        public EntityLink(Guid entityId) => EntityId = entityId;
                        public EntityLink<TResult> WithId(Guid id) => new EntityLink<TResult>(id);
                        public Guid EntityId { get; private set; }
                    }

                    ///<summary>Implement <see cref="ICreateMyOwnResultQuery{TResult}"/> by passing a func to this base class.</summary>
                    public abstract class FuncResultQuery<TResult> : Query<TResult>, ICreateMyOwnResultQuery<TResult>
                    {
                        readonly Func<TResult> _factory;
                        protected FuncResultQuery(Func<TResult> factory) => _factory = factory;
                        public TResult CreateResult() => _factory();
                    }

                    /// <summary>Implements <see cref="ICreateMyOwnResultQuery{TResult}"/> by calling the default constructor on <typeparamref name="TResult"/></summary>
                    public class NewableResultLink<TResult> : Query<TResult>, ICreateMyOwnResultQuery<TResult>
                    {
                        static readonly Func<TResult> Constructor = System.Reflection.Constructor.For<TResult>.DefaultConstructor.Instance;
                        public TResult CreateResult() => Constructor();
                    }
                }
            }

            public static partial class ExactlyOnce
            {
                public class Command : ValueObject<Command>, Remotable.ExactlyOnce.ICommand
                {
                    public Guid MessageId { get; private set; }

                    protected Command()
                        : this(Guid.NewGuid()) {}

                    Command(Guid id) => MessageId = id;
                }
            }
        }

        internal static void MapTypes(ITypeMappingRegistar typeMapper)
        {
            typeMapper
               .MapTypeAndStandardCollectionTypes<BusApi.Remotable.IEvent>("1E0DB1B4-71A6-4D2E-901F-E238ABA30B63");
        }
    }
}
