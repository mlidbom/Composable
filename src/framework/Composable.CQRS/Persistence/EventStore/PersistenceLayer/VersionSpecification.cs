using System;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    class VersionSpecification
    {
        public VersionSpecification(Guid eventId, int version)
        {
            EventId = eventId;
            EffectiveVersion = version;
        }

        public Guid EventId { get; }
        public int EffectiveVersion { get; }
    }
}
