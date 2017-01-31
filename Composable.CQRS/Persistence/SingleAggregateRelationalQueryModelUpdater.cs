using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;

namespace Composable.Persistence
{
    // ReSharper disable once UnusedMember.Global todo: write tests
    abstract class SingleAggregateRelationalQueryModelUpdater<TImplementer, TQueryModel, TRootEventHandled, TSession> :
        CallsMatchingHandlersInRegistrationOrderEventHandler<TRootEventHandled>
        where TImplementer : SingleAggregateRelationalQueryModelUpdater<TImplementer, TQueryModel, TRootEventHandled, TSession>
        where TRootEventHandled : class, IAggregateRootEvent
        where TQueryModel : ISingleAggregateQueryModel, new()
        where TSession : IEntityPersister
    {
        protected readonly TSession Session;
        protected TQueryModel Model { get; set; }

        protected SingleAggregateRelationalQueryModelUpdater(TSession session)
        {
            Session = session;

            RegisterHandlers()
                .BeforeHandlers(
                    e =>
                    {
                        if(e is IAggregateRootCreatedEvent)
                        {
                            Model = new TQueryModel();
                            Model.SetId(e.AggregateRootId);
                        }
                        else if(!(e is IAggregateRootDeletedEvent))
                        {
                            Model = Session.GetForUpdate<TQueryModel>(e.AggregateRootId);
                        }
                    })
                .AfterHandlers(
                    e =>
                    {
                        if(e is IAggregateRootCreatedEvent)
                        {
                            Session.Save(Model);
                        }
                    });
        }
    }
}
