using System;

namespace Composable.Messaging.Buses.Implementation
{
    public class MessageDispatchingFailedException : Exception
    {
        public MessageDispatchingFailedException(string remoteExceptionAsString) : base($@"Dispatching message failed. Remote exception message: 
##################################################
{remoteExceptionAsString} 
#################################################") {}
    }
}
