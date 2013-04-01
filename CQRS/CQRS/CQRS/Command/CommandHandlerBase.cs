using System;
using System.Transactions;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.Command
{
    public abstract class CommandHandlerBase<TCommand, TCommandSuccess, TCommandFailed> : IHandleMessages<TCommand>
        where TCommand : Command
        where TCommandSuccess : CommandSuccess, new()
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
                HandleCommand(message);
                SendSuccessMessage(message.Id);
            }
            catch (Exception e)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var evt = new TCommandFailed
                    {
                        CommandId = message.Id,
                        Message = e.Message,
                    };
                    _bus.Publish(evt);
                }

                if (e.GetType() != typeof(CommandFailedException))
                {
                    throw;
                }
            }
        }

        protected virtual void SendSuccessMessage(Guid messageId)
        {
            var evt = new TCommandSuccess
            {
                CommandId = messageId,
            };
            _bus.Publish(evt);
        }

        protected abstract void HandleCommand(TCommand command);
    }
}
