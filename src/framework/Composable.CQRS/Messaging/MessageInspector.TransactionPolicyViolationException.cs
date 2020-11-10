using System;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        public class TransactionPolicyViolationException : Exception
        {
            public TransactionPolicyViolationException(string message) : base(message + TransactionPolicyRationale) {}

            static readonly string TransactionPolicyRationale = $@"

Rationale: 
When accessing services on a bus it is very important to understand whether or not the service you are accessing will behave transactionally or not. 
If you make a mistake with regards to this you are likely to end up intermittent bugs resulting in corrupt data. 
Such bugs are notoriously hard both to debug and to reproduce at all.

In order to minimize your risk of encountering such problems we have runtime validations detecting if your usage of transactions in combination with services make sense.

Rules: 

* Within a transaction you are not allowed send any {typeof(ICannotBeSentRemotelyFromWithinTransaction).FullName}
These messages will not respect the transaction so if you want to send them during a transaction you have to acknowledge that they will not by suppressing the current transaction before sending them.


* There must be a transaction for you to send a {typeof(IMustBeSentTransactionally).FullName}
The whole point of these message types is to guarantee you exactly once delivery. Without a transaction while sending this guarantee is lost.

";
        }
    }
}
