using System;
using System.Diagnostics.Contracts;

namespace Composable.CQRS
{
    [ContractClass(typeof(CommandServiceContract))]
    public interface ICommandService
    {
        void Execute<TCommand>(TCommand command);
    }

    [ContractClassFor(typeof(ICommandService))]
    internal abstract class CommandServiceContract : ICommandService
    {
        public void Execute<TCommand>(TCommand command)
        {
            Contract.Requires(command!=null);
            throw new NotImplementedException();
        }
    }
}