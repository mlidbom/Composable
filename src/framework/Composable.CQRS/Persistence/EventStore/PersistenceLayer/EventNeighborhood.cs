namespace Composable.Persistence.EventStore.PersistenceLayer
{
    class EventNeighborhood
    {
        public ReadOrder EffectiveReadOrder { get; }
        public ReadOrder PreviousEventReadOrder { get; }
        public ReadOrder NextEventReadOrder { get; }

        public EventNeighborhood(ReadOrder effectiveReadOrder, ReadOrder? previousEventReadOrder, ReadOrder? nextEventReadOrder)
        {
            EffectiveReadOrder = effectiveReadOrder;
            NextEventReadOrder = nextEventReadOrder ?? ReadOrder.FromLong(EffectiveReadOrder.Order + 1);
            PreviousEventReadOrder = previousEventReadOrder ?? ReadOrder.Zero;
        }
    }
}
