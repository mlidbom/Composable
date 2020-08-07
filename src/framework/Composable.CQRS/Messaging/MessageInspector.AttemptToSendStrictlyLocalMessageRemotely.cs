using System;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        public class AttemptToSendStrictlyLocalMessageRemotelyException : Exception
        {
            public AttemptToSendStrictlyLocalMessageRemotelyException(MessageTypes.StrictlyLocal.IMessage message) : base(RemoteSendOfStrictlyLocalMessageMessage(message)) {}

            static string RemoteSendOfStrictlyLocalMessageMessage(MessageTypes.StrictlyLocal.IMessage message) => $@"

{message.GetType().FullName} cannot be sent remotely because it implements {typeof(MessageTypes.StrictlyLocal.IMessage)}.

Rationale: 
{typeof(MessageTypes.StrictlyLocal.IMessage)} implementations are designed explicitly to be used locally.
The result of sending them off remotely is unclear to say the least and very unlikely to end up doing what you want. 
";
        }
    }
}
