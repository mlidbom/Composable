using System;
using System.Transactions;
using Composable.System;
using Composable.System.Linq;
using Microsoft.Practices.ServiceLocation;
using System.Linq;

namespace Composable.CQRS
{
    public class CommandService : ICommandService
    {
        private readonly IServiceLocator _handlerProvider;

        public CommandService(IServiceLocator handlerProvider)
        {
            _handlerProvider = handlerProvider;
        }


        public virtual void Execute<TCommand>(TCommand command) where TCommand : IDomainCommand<TCommand>
        {
            using(var transaction = new TransactionScope())
            {
                //var handlers = _handlerProvider.GetAllInstances<ICommandHandler<TCommand>>();
                //if(handlers.Count() > 1)
                //{
                //    throw new Exception("More than one registered handler for command: {0}".FormatWith(typeof(TCommand)));
                //}if(handlers.None())
                //{
                //    throw new Exception("No handler registered for {0}".FormatWith(typeof(TCommand)));
                //}

                //handlers.Single().Execute(command);

                _handlerProvider.GetInstance<ICommandHandler<TCommand>>().Execute(command);
                
                transaction.Complete();
            }
        }
    }
}