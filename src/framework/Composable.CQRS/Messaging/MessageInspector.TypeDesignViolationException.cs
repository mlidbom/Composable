using System;

namespace Composable.Messaging
{
    static partial class MessageTypeInspector
    {
        class MessageTypeDesignViolationException : Exception
        {
            public MessageTypeDesignViolationException(string message) : base(message + TypeDesignRationale) {}

            const string TypeDesignRationale = @"

Rationale: 
In order to provide reliable guarantees as to the behavior of services on the bus we must know the exact semantics of each message sent. 
Some combinations of inherited interfaces would present contradictions which would make it impossible for the bus to know how to act.
Some inherited interfaces absolutely require that concrete types implement some other interface.
It is quite easy to miss this when designing your types unless you have help.
We provide this help by detecting these mistakes and throwing runtime exceptions.
";
        }
    }
}
