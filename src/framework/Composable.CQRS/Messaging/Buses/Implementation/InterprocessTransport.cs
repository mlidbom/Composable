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
            foreach(var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    Contract.Argument.Assert(!IsCommand(messageType), !IsQuery(messageType));
                    _eventRoutes.GetOrAdd(messageType, () => new List<IInbox>()).Add(inbox);
                } else if(typeof(ICommand).IsAssignableFrom(messageType))
                {
                    Contract.Argument.Assert(!IsEvent(messageType), !IsQuery(messageType), !_commandRoutes.ContainsKey(messageType));
                    _commandRoutes.Add(messageType, inbox);
                } else if(typeof(IQuery).IsAssignableFrom(messageType))
                {
                    Contract.Argument.Assert(!IsEvent(messageType), !IsCommand(messageType), !_queryRoutes.ContainsKey(messageType));
                    _queryRoutes.Add(messageType, inbox);
                }
            }
        }

        static bool IsCommand(Type type) => typeof(ICommand).IsAssignableFrom(type);
        static bool IsEvent(Type type) => typeof(IEvent).IsAssignableFrom(type);
        static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);

        public void Dispatch(IEvent @event) => _eventRoutes[@event.GetType()].ForEach(inbox => inbox.Dispatch(@event));
        public void Dispatch(ICommand command) => _commandRoutes[command.GetType()].Dispatch(command);
        public async Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage => (TCommandResult)await _commandRoutes[command.GetType()].Dispatch(command);
        public async Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query) where TQueryResult : IQueryResult => (TQueryResult)await _queryRoutes[query.GetType()].Dispatch(query);
    }
}
