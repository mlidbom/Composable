#region usings

using System;
using System.Diagnostics.Contracts;
using System.Transactions;
using Composable.DomainEvents;
using Composable.System.Linq;
using Microsoft.Practices.ServiceLocation;

#endregion

namespace Composable.CQRS.Command
{
    public class CommandService : ICommandService
    {
        private readonly IServiceLocator _serviceLocator;

        public CommandService(IServiceLocator serviceLocator)
        {
            Contract.Requires(serviceLocator != null);
            _serviceLocator = serviceLocator;
        }

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_serviceLocator != null);
        }

        private void ExecuteSingle<TCommand>(TCommand command) {
            var handler = _serviceLocator.GetInstance<ICommandHandler<TCommand>>();
            handler.Execute(command);
        }

        public virtual CommandResult Execute<TCommand>(TCommand command)
        {
            var result = new CommandResult();
#pragma warning disable 612,618
            using(DomainEvent.RegisterShortTermSynchronousListener<IDomainEvent>(result.RegisterEvent))
#pragma warning restore 612,618
            {
                using(var transaction = new TransactionScope())
                {
                    if (command is CompositeCommand) {
                        (command as CompositeCommand).GetContainedCommands().ForEach(c => { ExecuteSingle((dynamic)c); });
                    }
                    else
                        ExecuteSingle(command);

                    transaction.Complete();
                }
                return result;
            }
        }
    }
}