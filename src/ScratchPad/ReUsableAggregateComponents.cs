using System.Collections.Generic;
using Composable.Persistence.EventStore;
// ReSharper disable IdentifierTypo
// ReSharper disable NotAccessedVariable
#pragma warning disable 219
#pragma warning disable 414

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
        static void DemonstrateSemanticRelationships()
        {

            IAggregate1ComponentEvent<IComponentEventBase> wceb = null!;
            IAggregate1ComponentEvent<IComponentEvent1> wce1 = null!;
            IAggregate1ComponentEvent<IComponentEvent2> wce2 = null!;

            //Semantic relationship is maintained.
            //For registering handlers we could enable registering via the wrapped type so that handlers need not always do the unwrapping.
            wceb = wce1 = wce2;

        }
    }
}
