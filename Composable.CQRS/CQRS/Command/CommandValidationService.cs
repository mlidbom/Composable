#region usings

using System.Collections.Generic;
using System.Transactions;
using Microsoft.Practices.ServiceLocation;
using System.Linq;

#endregion

namespace Composable.CQRS.Command
{
    public class CommandValidationService : ICommandValidationService
    {
        private readonly IServiceLocator _serviceLocator;

        public CommandValidationService(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public IEnumerable<IValidationFailure> Validate<TCommand>(TCommand command)
        {
            IEnumerable<IValidationFailure> failures;
            using(var transaction = new TransactionScope())
            {
                var handlers = _serviceLocator.GetAllInstances<ICommandValidator<TCommand>>().ToArray();
                failures = handlers.SelectMany(handler => handler.Validate(command));

                transaction.Complete();
            }
            return failures;
        }
    }
}