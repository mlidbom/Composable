using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    class InterprocessTransport : IInterprocessTransport
    {
        readonly Dictionary<Type, IList<IInbox>> _eventRoutes = new Dictionary<Type, IList<IInbox>>();
        readonly Dictionary<Type, IInbox> _commandRoutes = new Dictionary<Type, IInbox>();
        readonly Dictionary<Type, IInbox> _queryRoutes = new Dictionary<Type, IInbox>();

        public void Connect(IEndpoint endpoint)
        {
            IMessageHandlerRegistry messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();
            var inbox = endpoint.ServiceLocator.Resolve<IInbox>();
            foreach (var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    Contract.Invariant.Assert(!IsCommand(messageType), !IsQuery(messageType));
                    _eventRoutes.GetOrAdd(messageType, () => new List<IInbox>()).Add(inbox);
                }else if(typeof(ICommand).IsAssignableFrom(messageType))
                {
                    Contract.Invariant.Assert(!IsEvent(messageType), !IsQuery(messageType), !_commandRoutes.ContainsKey(messageType));
                    _commandRoutes.Add(messageType, inbox);
                }
                else if(typeof(IQuery).IsAssignableFrom(messageType))
                {
                    Contract.Invariant.Assert(!IsEvent(messageType), !IsCommand(messageType), !_queryRoutes.ContainsKey(messageType));
                    _queryRoutes.Add(messageType, inbox);
                }
            }
        }

        static bool IsCommand(Type type) => typeof(ICommand).IsAssignableFrom(type);
        static bool IsEvent(Type type) => typeof(IEvent).IsAssignableFrom(type);
        static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);



        public Task<object> Dispatch(IMessage message)
        {
            switch(message)
            {
                case ICommand command:
                    return _commandRoutes[message.GetType()].Dispatch(command);
                case IEvent @event:
                    _eventRoutes[message.GetType()].ForEach(inbox => inbox.Dispatch(@event));
                    return Task.FromResult((object)null);
                case IQuery query:
                    return _queryRoutes[query.GetType()].Dispatch(query);
               default:
                    throw new Exception($"Unsupported message type: {message.GetType()}");
            }
        }
    }
}