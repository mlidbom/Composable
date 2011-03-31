#region usings

using System.Diagnostics.Contracts;
using Composable.Persistence;

#endregion

namespace Composable.CQRS
{
    public class EntityCommandHandler<TEntity, TCommand> : ICommandHandler<TCommand>
        where TEntity : IEntityCommandHandler<TCommand>
        where TCommand : IEntityHandledCommand
    {
        private readonly IEntityReader _session;

        protected EntityCommandHandler(IEntityReader session)
        {
            Contract.Requires(session != null);
            _session = session;
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(_session != null);
        }

        public void Execute(TCommand command)
        {
            _session.Get<TEntity>(command.EntityId).Execute(command);
        }
    }
}