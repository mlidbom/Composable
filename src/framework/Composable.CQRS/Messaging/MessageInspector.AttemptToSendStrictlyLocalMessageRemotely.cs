using System;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        public class AttemptToSendStrictlyLocalMessageRemotelyException : Exception
        {
            public AttemptToSendStrictlyLocalMessageRemotelyException(IStrictlyLocalMessage message) : base(RemoteSendOfStrictlyLocalMessageMessage(message)) {}

            static string RemoteSendOfStrictlyLocalMessageMessage(IStrictlyLocalMessage message) => $@"

{message.GetType().FullName} cannot be sent remotely because it implements {typeof(IStrictlyLocalMessage)}.

Rationale: 
{typeof(IStrictlyLocalMessage)} implementations are designed explicitly to be used locally.
The result of sending them off remotely is unclear to say the least and very unlikely to end up doing what you want. 
";
        }
    }
}
