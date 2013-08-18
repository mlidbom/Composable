using System;
using Composable.DDD;

namespace Composable.CQRS.EventHandling
{
    public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
    {
        void SetId(Guid id);
    }
}