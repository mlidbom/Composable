#region usings

using System;
using System.Linq;
using System.Transactions;
using Composable.System;
using Composable.System.Linq;
using Microsoft.Practices.ServiceLocation;

#endregion

namespace Composable.CQRS
{
    public class CommandService : ICommandService
    {
        private readonly IServiceLocator _serviceLocator;

        public CommandService(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public virtual void Execute<TCommand>(TCommand command)
        {
            using (var transaction = new TransactionScope())
            {
                var handlers = _serviceLocator.GetAllInstances<ICommandHandler<TCommand>>();
                if (handlers.Count() > 1)
                {
                    throw new Exception(
                        "More than one registered handler for command: {0}".FormatWith(typeof(TCommand)));
                }
                if (handlers.None())
                {
                    throw new Exception("No handler registered for {0}".FormatWith(typeof(TCommand)));
                }
                handlers.Single().Execute(command);
                transaction.Complete();
            }
        }
    }
}