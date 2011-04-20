using System.Transactions;
using Composable.System.Transactions;

namespace Composable.CQRS.Command
{
    public class ForceDistributedTransactionsCommandServiceDecorator : ICommandService
    {
        private readonly ICommandService _service;

        public ForceDistributedTransactionsCommandServiceDecorator(ICommandService service)
        {
            _service = service;
        }

        public virtual CommandResult Execute<TCommand>(TCommand command)
        {
            using (var transaction = new TransactionScope().EnsureDistributed())
            {                
                var result = _service.Execute(command);
                transaction.Complete();
                return result;
            }
        }
    }
}