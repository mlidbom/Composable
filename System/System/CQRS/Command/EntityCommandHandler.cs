using System.Diagnostics.Contracts;

namespace Composable.CQRS
{
    public class EntityCommandHandler<TEntity, TCommand> : ICommandHandler<TCommand>
        where TEntity : IEntityCommandHandler<TCommand>
        where TCommand : IEntityHandledCommand
    {
        private readonly IEntityFetcher _session;

        protected EntityCommandHandler(IEntityFetcher session)
        {
            Contract.Requires(session != null);
            _session = session;
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(_session!=null);
        }

        public void Execute(TCommand command)
        {
            _session.Get<TEntity>(command.EntityId).Execute(command);
        }
    }
}