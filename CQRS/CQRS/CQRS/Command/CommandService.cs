#region usings

using System;
using System.Diagnostics.Contracts;
using System.Transactions;
using Composable.DomainEvents;
using Composable.System.Linq;
using Microsoft.Practices.ServiceLocation;
using System.Linq;

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

        protected virtual void ExecuteSingle<TCommand>(TCommand command) {
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
                    if (command is CompositeCommand) 
                    {
                        foreach(var subCommand in (command as CompositeCommand).GetContainedCommands())
                        {
                            try
                            {
                                ExecuteSingle((dynamic)subCommand.Command);
                            }
                            catch(CommandFailedException exception)
                            {
                                var failedException = new CommandFailedException(exception.Message, 
                                    exception.InvalidMembers
                                        .Select(invalidMember => subCommand.Name + "." + invalidMember)
                                        .ToList());
                                throw failedException;
                            }
                        }
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