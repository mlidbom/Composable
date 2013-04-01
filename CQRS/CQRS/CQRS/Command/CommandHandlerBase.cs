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
        private TCommandSuccess evt;

        protected CommandHandlerBase(IServiceBus bus)
        {
            _bus = bus;
        }

        public void Handle(TCommand message)
        {
            try
            {
                HandleCommand(message);

                if (evt == null)
                {       
                    evt = new TCommandSuccess
                    {
                        CommandId = message.Id,
                    };
                }

                _bus.Publish(evt);
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

        protected  void SetSuccessEvent(TCommandSuccess _event)
        {
            evt = _event;
        }

        protected abstract void HandleCommand(TCommand command);
    }

 
}
