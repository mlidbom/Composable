using Composable.Persistence.EventStore;
// ReSharper disable All
#pragma warning disable 414
#pragma warning disable IDE0059 // Unnecessary assignment of a value

namespace ScratchPad
{
    //todo: Implement the ability to use this pattern in the aggregate root and ensure that routing on the bus also work correctly.
    interface IAggregate1Event : IAggregateEvent{}

    interface IAggregate1ComponentEvent<out TComponentEvent> : IAggregateEvent{}

    interface IComponentEventBase{}

    interface IComponentEvent1 : IComponentEventBase
    {
    }

    interface IComponentEvent2 : IComponentEvent1
    {
    }

    public class ReUsableAggregateComponents
    {
#pragma warning disable IDE0051 // Remove unused private members
        static void DemonstrateSemanticRelationships()
        {
            IAggregate1ComponentEvent<IComponentEventBase> wceb = null!;
            IAggregate1ComponentEvent<IComponentEvent1> wce1 = null!;
            IAggregate1ComponentEvent<IComponentEvent2> wce2 = null!;

            //Semantic relationship is maintained.
            //For registering handlers we could enable registering via the wrapped type so that handlers need not always do the unwrapping.
            //It would be possible to listen to all component events, regardless of the owning aggregate type, or to zoom in on specific aggregate's component events.
            wceb = wce1 = wce2;

        }
    }
}
