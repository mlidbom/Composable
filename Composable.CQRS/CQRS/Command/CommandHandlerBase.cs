using System;
using System.Linq;
using System.Transactions;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.Command
{
    public abstract class CommandHandlerBase<TCommand> : IHandleMessages<TCommand>
        where TCommand : ICommand
    {
        private readonly IServiceBus _bus;
        private readonly ICommandService _commandService;

        protected CommandHandlerBase(IServiceBus bus, ICommandService commandService)
        {
            _bus = bus;
            _commandService = commandService;
        }

        public void Handle(TCommand command)
        {
            try
            {
                //Mmmm. Dynamic is not usually OK, but here we want the inheritor to be able to specify an interface or base class, as the 
                //message to be listened to, while still correctly dispatching the command to the correct handler...
                var commandResult = (CommandResult)_commandService.Execute((dynamic)command);

                _bus.Reply(new CommandSuccessResponse(commandId:command.Id, events: commandResult.Events.ToArray()));
            }
            catch(Exception e)
            {
                using(new TransactionScope(TransactionScopeOption.Suppress))
                {
                    if(e is DomainCommandValidationException)
                    {
                        var commandFailedException = e as DomainCommandValidationException;
                        _bus.Reply(new CommandDomainValidationExceptionResponse
                                   {
                                       CommandId = command.Id,
                                       Message = commandFailedException.Message,
                                       InvalidMembers = commandFailedException.InvalidMembers.ToList()
                                   });
                    }
                    //todo:Try to get retries working sanely here since it may be an intermittent error such as a timeout or deadlock etc...
                    _bus.Reply(new CommandExecutionExceptionResponse
                               {
                                   CommandId = command.Id
                               });
                }
                throw; //This currently requires that retries is set to 0 to behave correctly.
            }
        }
    }
}
