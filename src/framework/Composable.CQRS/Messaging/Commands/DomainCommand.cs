using System;
using Composable.DDD;

namespace Composable.Messaging.Commands
{
    public class TransactionalExactlyOnceDeliveryCommand : ValueObject<TransactionalExactlyOnceDeliveryCommand>, ITransactionalExactlyOnceDeliveryCommand
    {
        public Guid MessageId { get; private set; }

        protected TransactionalExactlyOnceDeliveryCommand()
            : this(Guid.NewGuid()) {}

        TransactionalExactlyOnceDeliveryCommand(Guid id) => MessageId = id;
    }

    public class TransactionalExactlyOnceDeliveryCommand<TResult> : TransactionalExactlyOnceDeliveryCommand, ITransactionalExactlyOnceDeliveryCommand<TResult> {}
}
