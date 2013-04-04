using System;
using System.Transactions;
using Composable.ServiceBus;
using NServiceBus;
using NServiceBus.Unicast.Transport;

namespace Composable.CQRS.Command
{
    public abstract class CommandHandlerBase<TCommand, TCommandFailed> : IHandleMessages<TCommand>
        where TCommand : ICommand
        where TCommandFailed : CommandFailed, new()
    {
        private readonly IServiceBus _bus;

        protected CommandHandlerBase(IServiceBus bus)
        {
            _bus = bus;
        }

        public void Handle(TCommand message)
        {
            IBus bus = null;
            try
            {
                HandleCommand(message);
            }
            catch (Exception e)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var commandFailed = CreateCommandFailedException(e, message);
                    _bus.Publish(commandFailed);
                }
                //Make sure that NServiceBus will roll back the transaction and throw this message away rather than move it to the error queue.
                throw new AbortHandlingCurrentMessageException();
            }
        }

        protected virtual TCommandFailed CreateCommandFailedException(Exception e, TCommand message)
        {
            return new TCommandFailed
            {
                CommandId = message.Id,
                Message = e.Message,
            };
        }
        protected abstract void HandleCommand(TCommand command);
    }


}
