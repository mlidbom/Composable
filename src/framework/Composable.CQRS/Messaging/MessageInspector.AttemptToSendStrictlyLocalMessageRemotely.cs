using System;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        class AttemptToSendStrictlyLocalMessageRemotely : Exception
        {
            public AttemptToSendStrictlyLocalMessageRemotely(BusApi.StrictlyLocal.IMessage message) : base(RemoteSendOfStrictlyLocalMessageMessage(message)) {}

            static string RemoteSendOfStrictlyLocalMessageMessage(BusApi.StrictlyLocal.IMessage message) => $@"

{message.GetType().FullName} cannot be sent remotely because it implements {typeof(BusApi.StrictlyLocal.IMessage)}.

Rationale: 
{typeof(BusApi.StrictlyLocal.IMessage)} implementations are designed explicitly to be used locally.
The result of sending them off remotely is unclear to say the least and very unlikely to end up doing what you want. 
";
        }
    }
}
