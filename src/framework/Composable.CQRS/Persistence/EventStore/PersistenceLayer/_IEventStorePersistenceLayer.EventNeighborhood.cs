namespace Composable.Persistence.EventStore.PersistenceLayer
{
    partial interface IEventStorePersistenceLayer
    {
        class EventNeighborhood
        {
            public ReadOrder EffectiveReadOrder { get; }
            public ReadOrder PreviousEventReadOrder { get; }
            public ReadOrder NextEventReadOrder { get; }

            public EventNeighborhood(ReadOrder effectiveReadOrder, ReadOrder? previousEventReadOrder, ReadOrder? nextEventReadOrder)
            {
                EffectiveReadOrder = effectiveReadOrder;
                NextEventReadOrder = nextEventReadOrder ?? new ReadOrder(EffectiveReadOrder.Order + 1, offSet: 0);
                PreviousEventReadOrder = previousEventReadOrder ?? ReadOrder.Zero;
            }
        }
    }
}
