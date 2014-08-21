using System;
using Composable.ServiceBus;
using Composable.System;
using NServiceBus;
using NServiceBus.MessageMutator;
using NServiceBus.Saga;
using log4net;

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public class MessageSourceValidator : IMutateIncomingMessages
    {
        private static ILog Log = LogManager.GetLogger(typeof(MessageSourceValidator));

        private readonly IBus _bus;
        public MessageSourceValidator(IBus bus)
        {
            _bus = bus;
        }

        public object MutateIncoming(object message)
        {
            string environmentName;

            if (message is IAmTimeoutMessage)
            {
                //Message is a timeout message that is sent internally from nServiceBus and therefore has no environmentheading
                var timeout = (IAmTimeoutMessage)message;
                if (timeout.EnvironmentName != EndpointCfg.EnvironmentName)
                {
                    throw new Exception("Recieved message from other environment: {0} in environment {1}".FormatWith(timeout.EnvironmentName, EndpointCfg.EnvironmentName));
                }
                return message;
            }

            if (!_bus.CurrentMessageContext.Headers.TryGetValue(EndpointCfg.EnvironmentNameMessageHeaderName, out environmentName))
            {
                throw new Exception("Recived message without an environment header.");
            }

            if (environmentName != EndpointCfg.EnvironmentName)
            {
                throw new Exception("Recieved message from other environment: {0} in environment {1}".FormatWith(environmentName, EndpointCfg.EnvironmentName));
            }
            return message;
        }
    }
}