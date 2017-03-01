#region usings

using System.Diagnostics.Contracts;
using Castle.Windsor;
using Composable.DomainEvents;
using System.Linq;
using Composable.Windsor;

#endregion

namespace Composable.CQRS.Command
{
  using Composable.Messaging.Commands;

  public class CommandService : ICommandService
    {
        readonly IWindsorContainer _container;

        public CommandService(IWindsorContainer container)
        {
            Contract.Requires(container != null);
            _container = container;
        }

        [ContractInvariantMethod] void Invariant()
        {
            Contract.Invariant(_container != null);
        }

        protected virtual void ExecuteSingle<TCommand>(TCommand command) {
            _container.UseComponent<ICommandHandler<TCommand>>(
                    handler => handler.Execute(command)
                );
        }

        public virtual CommandResult Execute<TCommand>(TCommand command)
        {
            var result = new CommandResult();
#pragma warning disable 612,618
            using(DomainEvent.RegisterShortTermSynchronousListener<IDomainEvent>(result.RegisterEvent))
#pragma warning restore 612,618
            {
                using(var transaction = _container.BeginTransactionalUnitOfWorkScope())
                {
                    if (command is CompositeCommand) 
                    {
                        foreach(var subCommand in (command as CompositeCommand).GetContainedCommands())
                        {
                            try
                            {
                                ExecuteSingle((dynamic)subCommand.Command);
                            }
                            catch (DomainCommandValidationException exception)
                            {
                                var failedException = new DomainCommandValidationException(exception.Message, 
                                    exception.InvalidMembers
                                        .Select(invalidMember => subCommand.Name + "." + invalidMember)
                                        .ToList());
                                throw failedException;
                            }
                        }
                    }
                    else
                        ExecuteSingle(command);

                    transaction.Commit();
                }
                return result;
            }
        }
    }
}