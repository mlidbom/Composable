using System;
using Composable.DDD;

namespace Composable.Messaging.Events
{
    public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
    {
        void SetId(Guid id);
    }
}
