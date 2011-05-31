#region usings

using System;
using System.Diagnostics.Contracts;
using Composable.CQRS.EventSourcing;
using Composable.Persistence;

#endregion

namespace Composable.CQRS
{
    public class EntityCommandHandler<TEntity, TCommand> : ICommandHandler<TCommand>
        where TEntity : AggregateRoot<TEntity>, IEntityCommandHandler<TCommand>
        where TCommand : IEntityHandledCommand
    {
        private readonly IEventStoreSession _session;

        protected EntityCommandHandler(IEventStoreSession session)
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
            _session.Get<TEntity>((Guid)command.EntityId).Execute(command);
            _session.SaveChanges();
        }
    }
}