using System;
using System.Diagnostics.Contracts;

namespace Composable.CQRS
{
    [ContractClass(typeof(CommandServiceContract))]
    public interface ICommandService
    {
        CommandResult Execute<TCommand>(TCommand command);
    }

    [ContractClassFor(typeof(ICommandService))]
    internal abstract class CommandServiceContract : ICommandService
    {
        public CommandResult Execute<TCommand>(TCommand command)
        {
            Contract.Requires(command!=null);
            Contract.Ensures(Contract.Result<CommandResult>()!=null);
            throw new NotImplementedException();
        }
    }
}