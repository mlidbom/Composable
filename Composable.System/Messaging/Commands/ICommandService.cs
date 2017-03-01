#region usings



#endregion

namespace Composable.Messaging.Commands
{
  using global::System.Diagnostics.Contracts;

  using JetBrains.Annotations;

  [UsedImplicitly]
    [ContractClass(typeof(CommandServiceContract))]
    public interface ICommandService
    {
        CommandResult Execute<TCommand>(TCommand command);
    }

    [ContractClassFor(typeof(ICommandService))] abstract class CommandServiceContract : ICommandService
    {
        public CommandResult Execute<TCommand>(TCommand command)
        {
            Contract.Requires(command != null);
            Contract.Ensures(Contract.Result<CommandResult>() != null);
            return null;
        }
    }
}