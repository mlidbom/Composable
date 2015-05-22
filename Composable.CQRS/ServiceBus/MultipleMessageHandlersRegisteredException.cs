using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Linq;

namespace Composable.ServiceBus
{
    public class MultipleMessageHandlersRegisteredException : Exception
    {
        public MultipleMessageHandlersRegisteredException(object message, List<object> handlers)
            : base(CreateMessage(message, handlers)) {}

        private static string CreateMessage(object message, List<object> handlers)
        {
            var exceptionMessage = "There are multiple handlers registered for the message type:{0}.\nIf you are getting this because you have registered a listener as a test spy have your listener implement ISynchronousBusMessageSpy and the exception will disappear"
                .FormatWith(message.GetType());
            handlers.Select(handler => handler.GetType())
                .ForEach(handlerType => exceptionMessage += "{0}{1}".FormatWith(Environment.NewLine, handlerType.FullName)
                );
            return exceptionMessage;
        }
    }
}
