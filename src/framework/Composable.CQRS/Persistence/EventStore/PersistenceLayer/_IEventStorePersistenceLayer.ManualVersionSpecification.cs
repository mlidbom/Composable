using System;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    partial interface IEventStorePersistenceLayer
    {
        class ManualVersionSpecification
        {
            public ManualVersionSpecification(Guid eventId, int version)
            {
                EventId = eventId;
                EffectiveVersion = version;
            }

            public Guid EventId { get; }
            public int EffectiveVersion { get; }
        }
    }
}
