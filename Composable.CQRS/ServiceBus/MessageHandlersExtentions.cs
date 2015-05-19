using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.System.Linq;

namespace Composable.ServiceBus
{
    internal static class MessageHandlersExtentions
    {
        internal static void Invoke(this MessageHandlerResolver.MessageHandlers messageHandlers)
        {
            foreach (var handlerInstance in messageHandlers.HandlerInstances)
            {
                MessageHandlerInvoker.GetMethodsToInvoke(handlerInstance.GetType(), messageHandlers.HandlerInterfaceType)
                    .Where(holder => holder.HandledMessageType.IsInstanceOfType(messageHandlers.Message))
                    .Select(holder => holder.HandlerMethod)
                    .ForEach(handlerMethod => handlerMethod(handlerInstance, messageHandlers.Message));
            }
        }
    }
}
