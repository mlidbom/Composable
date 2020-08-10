using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Messaging.Events;
using Composable.Refactoring.Naming;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    //performance: Use static caching + indexing trick for storing and retrieving values throughout this class. QueryTypeIndexFor<TQuery>.Index. Etc
    class MessageHandlerRegistry : IMessageHandlerRegistrar, IMessageHandlerRegistry
    {
        readonly ITypeMapper _typeMapper;
        IReadOnlyDictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
        IReadOnlyDictionary<Type, IReadOnlyList<Action<MessageTypes.IEvent>>> _eventHandlers = new Dictionary<Type, IReadOnlyList<Action<MessageTypes.IEvent>>>();
        IReadOnlyDictionary<Type, HandlerWithResultRegistration> _queryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
        IReadOnlyDictionary<Type, HandlerWithResultRegistration> _commandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();
        IReadOnlyList<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

        readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

        public MessageHandlerRegistry(ITypeMapper typeMapper) => _typeMapper = typeMapper;

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler) => _monitor.Update(() =>
        {
            MessageInspector.AssertValid<TEvent>();
            _eventHandlers.TryGetValue(typeof(TEvent), out var currentEventSubscribers);
            currentEventSubscribers ??= new List<Action<MessageTypes.IEvent>>();

            ThreadSafe.AddToCopyAndReplace(ref _eventHandlers, typeof(TEvent), currentEventSubscribers.AddToCopy(@event => handler((TEvent)@event)));
            ThreadSafe.AddToCopyAndReplace(ref _eventHandlerRegistrations, new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
            return this;
        });

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler) => _monitor.Update(() =>
        {
            MessageInspector.AssertValid<TCommand>();

            if(typeof(TCommand).Implements(typeof(MessageTypes.ICommand<>)))
            {
                throw new Exception($"{typeof(TCommand)} expects a result. You must register a method that returns a result.");
            }

            ThreadSafe.AddToCopyAndReplace(ref _commandHandlers, typeof(TCommand), command => handler((TCommand)command));
            return this;
        });

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) => _monitor.Update(() =>
        {
            MessageInspector.AssertValid<TCommand>();

            ThreadSafe.AddToCopyAndReplace(ref _commandHandlersReturningResults, typeof(TCommand), new CommandHandlerWithResultRegistration<TCommand, TResult>(handler));
            return this;
        });

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) => _monitor.Update(() =>
        {
            MessageInspector.AssertValid<TQuery>();

            ThreadSafe.AddToCopyAndReplace(ref _queryHandlers, typeof(TQuery), new QueryHandlerRegistration<TQuery, TResult>(handler));
            return this;
        });

        Action<object> IMessageHandlerRegistry.GetCommandHandler(MessageTypes.ICommand message)
        {
            if(TryGetCommandHandler(message, out var handler)) return handler;

            throw new NoHandlerException(message.GetType());
        }

        bool TryGetCommandHandler(MessageTypes.ICommand message, [MaybeNullWhen(false)]out Action<object> handler) =>
            _commandHandlers.TryGetValue(message.GetType(), out handler);

        public Func<MessageTypes.ICommand, object> GetCommandHandlerWithReturnValue(Type commandType) => _commandHandlersReturningResults[commandType].HandlerMethod;

        public Action<MessageTypes.ICommand> GetCommandHandler(Type commandType) => _commandHandlers[commandType];

        public Func<MessageTypes.IQuery<object>, object> GetQueryHandler(Type queryType) => _queryHandlers[queryType].HandlerMethod;

        public IReadOnlyList<Action<MessageTypes.IEvent>> GetEventHandlers(Type eventType)
        {
            //performance: Use static caching trick.
            return _eventHandlers.Where(@this => @this.Key.IsAssignableFrom(eventType)).SelectMany(@this => @this.Value).ToList();
        }

        public Func<MessageTypes.StrictlyLocal.IQuery<TQuery, TResult>, TResult> GetQueryHandler<TQuery, TResult>(MessageTypes.StrictlyLocal.IQuery<TQuery, TResult> query) where TQuery : MessageTypes.StrictlyLocal.IQuery<TQuery, TResult>
        {
            //Urgent: If we don't actually use the TQuery type parameter to do static caching here, remove it.
            if(_queryHandlers.TryGetValue(query.GetType(), out var handler))
            {
                return actualQuery => (TResult)handler.HandlerMethod(actualQuery);
            }

            throw new NoHandlerException(query.GetType());
        }

        public Func<MessageTypes.ICommand<TResult>, TResult> GetCommandHandler<TResult>(MessageTypes.ICommand<TResult> command)
        {
            if(_commandHandlersReturningResults.TryGetValue(command.GetType(), out var handler))
            {
                return actualQuery => (TResult)handler.HandlerMethod(actualQuery);
            }

            throw new NoHandlerException(command.GetType());
        }

        IEventDispatcher<MessageTypes.IEvent> IMessageHandlerRegistry.CreateEventDispatcher()
        {
            var dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<MessageTypes.IEvent>();
            var registrar = dispatcher.RegisterHandlers()
                                      .IgnoreUnhandled<MessageTypes.IEvent>();

            _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));

            return dispatcher;
        }

        public ISet<TypeId> HandledRemoteMessageTypeIds()
        {
            var handledTypes = _commandHandlers.Keys
                                               .Concat(_commandHandlersReturningResults.Keys)
                                               .Concat(_queryHandlers.Keys)
                                               .Concat(_eventHandlerRegistrations.Select(reg => reg.Type))
                                               .Where(messageType => messageType.Implements<MessageTypes.Remotable.IMessage>())
                                               .Where(messageType => !messageType.Implements<MessageTypes.Internal.IMessage>())
                                               .ToSet();

            var remoteResultTypes = _commandHandlersReturningResults
                                   .Where(handler => handler.Key.Implements<MessageTypes.Remotable.IMessage>())
                                   .Select(handler => handler.Value.ReturnValueType)
                                   .ToList();

            var typesNeedingMappings = handledTypes.Concat(remoteResultTypes);

            _typeMapper.AssertMappingsExistFor(typesNeedingMappings);

            return handledTypes.Select(_typeMapper.GetId)
                               .ToSet();
        }

        class EventHandlerRegistration
        {
            public Type Type { get; }
            public Action<IEventHandlerRegistrar<MessageTypes.IEvent>> RegisterHandlerWithRegistrar { get; }
            public EventHandlerRegistration(Type type, Action<IEventHandlerRegistrar<MessageTypes.IEvent>> registerHandlerWithRegistrar)
            {
                Type = type;
                RegisterHandlerWithRegistrar = registerHandlerWithRegistrar;
            }
        }

        abstract class HandlerWithResultRegistration
        {
            protected HandlerWithResultRegistration(Type returnValueType, Func<object, object> handlerMethod)
            {
                ReturnValueType = returnValueType;
                HandlerMethod = handlerMethod;
            }

            internal Type ReturnValueType { get; }
            internal Func<object, object> HandlerMethod { get; }
        }

        class CommandHandlerWithResultRegistration<TCommand, TResult> : HandlerWithResultRegistration
        {
            public CommandHandlerWithResultRegistration(Func<TCommand, TResult> handlerMethod)
                : base(typeof(TResult),
                       command => handlerMethod((TCommand)command) ?? throw new Exception("You cannot return null from a command handler")) {}
        }

        class QueryHandlerRegistration<TQuery, TResult> : HandlerWithResultRegistration
        {
            public QueryHandlerRegistration(Func<TQuery, TResult> handlerMethod)
                : base(typeof(TResult),
                       command => handlerMethod((TQuery)command) ?? throw new Exception("You cannot return null from a query handler")) {}
        }
    }
}
