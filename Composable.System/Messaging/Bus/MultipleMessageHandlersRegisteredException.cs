namespace Composable.Messaging.Bus
{
  using Composable.System;
  using Composable.System.Linq;

  using global::System;
  using global::System.Collections.Generic;

  public class MultipleMessageHandlersRegisteredException : Exception
    {
        public MultipleMessageHandlersRegisteredException(object message, IEnumerable<Type> handlerTypes)
            : base(CreateMessage(message, handlerTypes)) { }

        static string CreateMessage(object message, IEnumerable<Type> handlerTypes)
        {
            var exceptionMessage = "There are multiple handlers registered for the message type:{0}.\nIf you are getting this because you have registered a listener as a test spy have your listener implement ISynchronousBusMessageSpy and the exception will disappear"
                .FormatWith(message.GetType());
            handlerTypes.ForEach(handlerType => exceptionMessage += "{0}{1}".FormatWith(Environment.NewLine, handlerType.FullName));
            return exceptionMessage;
        }
    }
}
