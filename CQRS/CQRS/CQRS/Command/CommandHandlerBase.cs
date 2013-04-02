using System;
using System.Transactions;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.Command
{
    public abstract class CommandHandlerBase<TCommand, TCommandSuccess, TCommandFailed> : IHandleMessages<TCommand>
        where TCommand : Command
        where TCommandSuccess : CommandSuccess
        where TCommandFailed : CommandFailed, new()
    {
        private readonly IServiceBus _bus;

        protected CommandHandlerBase(IServiceBus bus)
        {
            _bus = bus;
        }

        public void Handle(TCommand message)
        {
            try
            {
                var commandSuccess = HandleCommand(message);
                _bus.Reply(commandSuccess);
            }
            catch (Exception e)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var commandFailed = new TCommandFailed
                    {
                        CommandId = message.Id,
                        Message = e.Message,
                    };
                    _bus.Reply(commandFailed);
                }
                //Always throw uncaught Exceptions so that surrounding infrastructure can handle it
                throw;
            }
        }
        protected abstract TCommandSuccess HandleCommand(TCommand command);
    }


}
