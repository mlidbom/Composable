namespace Composable.CQRS
{
    public class EntityCommandHandler<TEntity, TCommand> : ICommandHandler<TCommand>
        where TEntity : IEntityCommandHandler<TCommand>
        where TCommand : IEntityHandledCommand
    {
        private readonly IEntityFetcher _session;

        protected EntityCommandHandler(IEntityFetcher session)
        {
            _session = session;
        }

        public void Execute(TCommand command)
        {
            _session.Get<TEntity>(command.EntityId).Execute(command);
        }
    }
}