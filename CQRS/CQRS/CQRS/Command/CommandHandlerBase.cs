using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.ServiceBus;
using Composable.System.Linq;
using NServiceBus;
using NServiceBus.Unicast.Transport;
using System.Linq;

namespace Composable.CQRS.Command
{
    public abstract class CommandHandlerBase<TCommand, TCommandFailed> : IHandleMessages<TCommand>
        where TCommand : ICommand
        where TCommandFailed : CommandFailedResponse, new()
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
            }
            catch (Exception e)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var commandFailed = CreateCommandFailedException(e, message);
                    _bus.Reply(commandFailed);
                }
                throw;//This currently requires that retries is set to 0 to behave correctly.
            }
        }

        protected virtual TCommandFailed CreateCommandFailedException(Exception e, TCommand message)
        {
            IEnumerable<string> invalidMembers = Seq.Create<string>();
            if(e is CommandFailedException)
            {
                invalidMembers = ((CommandFailedException)e).InvalidMembers;
            }

            return new TCommandFailed
            {
                CommandId = message.Id,
                Message = e.Message,
                InvalidMembers = invalidMembers.ToArray()
            };
        }
        protected abstract void HandleCommand(TCommand command);
    }


}
