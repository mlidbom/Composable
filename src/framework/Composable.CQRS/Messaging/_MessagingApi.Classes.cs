using System;
using Composable.DDD;

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
            }
        }
    }
}
