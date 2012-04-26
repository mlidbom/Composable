using System;
using Composable.System;
using NServiceBus;
using log4net;

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public class MessageSourceValidator : IMessageHandler<IMessage>
    {
        private static ILog Log = LogManager.GetLogger(typeof(MessageSourceValidator));

        private readonly IBus _bus;
        public MessageSourceValidator(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(IMessage message)
        {
            string environmentName;
            if (!_bus.CurrentMessageContext.Headers.TryGetValue(EndpointCfg.EnvironmentNameMessageHeaderName, out environmentName))
            {
                throw new Exception("Recived message without an environment header.");
            }

            if (environmentName != EndpointCfg.EnvironmentName)
            {
                throw new Exception("Recieved message from other environment: {0} in environment {1}".FormatWith(environmentName, EndpointCfg.EnvironmentName));
            }
        }
    }
}