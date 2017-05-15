using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Composable.DependencyInjection;
using Composable.Messaging.Events;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses
{
    class MessageHandlerRegistry : IMessageHandlerRegistrar, IMessageHandlerRegistry
    {
        readonly Dictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
        readonly Dictionary<Type, Func<object,object>> _queryHandlers = new Dictionary<Type, Func<object, object>>();
        readonly List<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

        readonly object _lock = new object();
        IDependencyInjectionContainer _container;
        readonly IServiceLocator locator;

        public MessageHandlerRegistry(IDependencyInjectionContainer @this)
        {

            _container = @this;
            locator = _container.CreateServiceLocator();
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler)
        {
            lock(_lock)
            {
                _eventHandlerRegistrations.Add(new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler)
        {
            lock(_lock)
            {
                _commandHandlers.Add(typeof(TCommand), command => handler((TCommand)command));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForQuery<TQuery,TResult>(Func<TQuery, TResult> handler)
        {
            lock (_lock)
            {
                _queryHandlers.Add(typeof(TQuery), query => handler((TQuery)query));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.Handler<TMessageHandler>()
        {
            var messageHandlerType = typeof(TMessageHandler);

            _container.Register(Component.For<TMessageHandler>().ImplementedBy<TMessageHandler>().LifestyleScoped());

            var eventSubscriberInterfaces = messageHandlerType.GetInterfaces()
                .Where(@interface => @interface.Implements(typeof(IEventSubscriber<>)))
                .ToList();

            var commandHandlerInterfaces = messageHandlerType.GetInterfaces()
                                                .Where(@interface => @interface.Implements(typeof(ICommandHandler<>)))
                                                .ToList();

            var queryHandlerInterfaces = messageHandlerType.GetInterfaces()
                                               .Where(@interface => @interface.Implements(typeof(IQueryHandler<,>)))
                                               .ToList();

            var registrar = locator.Resolve<IMessageHandlerRegistrar>();
            var registrarExpression = Expression.Constant(registrar, typeof(IMessageHandlerRegistrar));

            //Create expression to resolve the handler instance from the container
            var locatorExpression = Expression.Constant(locator);
            var resolveMethodName = ExpressionUtil.ExtractMethodName(() => ServiceLocator.Resolve<object>(null));
            var resolveInstanceMethodCallExpression = Expression.Call(typeof(ServiceLocator), resolveMethodName, new[] { messageHandlerType }, locatorExpression);

            foreach (var @interface in eventSubscriberInterfaces)
            {
                var messageType = @interface.GenericTypeArguments.Single();

                //Create expression that calls the handler method given an event parameter.
                var eventParameterExpression = Expression.Parameter(messageType, "eventParameterName_irrelevant");
                var handleMethodName = ExpressionUtil.ExtractMethodName(() => ((IEventSubscriber<IEvent>)null).Handle(null));
                var handleMethodInformation = @interface.GetMethod(handleMethodName);
                var callHandleExpression = Expression.Call(resolveInstanceMethodCallExpression, handleMethodInformation, eventParameterExpression);

                //Create a lambda that takes an event parameter and calls the candler method with it.
                var eventHandlerLambda = Expression.Lambda(callHandleExpression, eventParameterExpression);

                //create expression to register the event handler
                var forEventMethodName = ExpressionUtil.ExtractMethodName(() => registrar.ForEvent<IEvent>(null));
                var forEventMethodCallExpression = Expression.Call(registrarExpression, forEventMethodName, new[] { messageType }, eventHandlerLambda);

                var registerLambda = Expression.Lambda(forEventMethodCallExpression);
                var registerMethod = (Func<IMessageHandlerRegistrar>)registerLambda.Compile();

                registerMethod();
            }

            foreach (var @interface in commandHandlerInterfaces)
            {
                var messageType = @interface.GenericTypeArguments.Single();

                //Create expression that calls the handler method given an event parameter.
                var commandParameterExpression = Expression.Parameter(messageType, "eventParameterName_irrelevant");
                var handleMethodName = ExpressionUtil.ExtractMethodName(() => ((ICommandHandler<ICommand>)null).Handle(null));
                var handleMethodInformation = @interface.GetMethod(handleMethodName);
                var callHandleExpression = Expression.Call(resolveInstanceMethodCallExpression, handleMethodInformation, commandParameterExpression);

                //Create a lambda that takes an event parameter and calls the candler method with it.
                var eventHandlerLambda = Expression.Lambda(callHandleExpression, commandParameterExpression);

                //create expression to register the command handler
                var forCommandMethodName = ExpressionUtil.ExtractMethodName(() => registrar.ForCommand<ICommand>(null));
                var forEventMethodCallExpression = Expression.Call(registrarExpression, forCommandMethodName, new[] { messageType }, eventHandlerLambda);

                var registerLambda = Expression.Lambda(forEventMethodCallExpression);
                var registerMethod = (Func<IMessageHandlerRegistrar>)registerLambda.Compile();

                registerMethod();
            }

            foreach (var @interface in queryHandlerInterfaces)
            {
                var messageType = @interface.GenericTypeArguments.Single();

                //Create expression that calls the handler method given an event parameter.
                var commandParameterExpression = Expression.Parameter(messageType, "eventParameterName_irrelevant");
                var handleMethodName = ExpressionUtil.ExtractMethodName(() => ((ICommandHandler<ICommand>)null).Handle(null));
                var handleMethodInformation = @interface.GetMethod(handleMethodName);
                var callHandleExpression = Expression.Call(resolveInstanceMethodCallExpression, handleMethodInformation, commandParameterExpression);

                //Create a lambda that takes an event parameter and calls the candler method with it.
                var eventHandlerLambda = Expression.Lambda(callHandleExpression, commandParameterExpression);

                //create expression to register the command handler
                var forCommandMethodName = ExpressionUtil.ExtractMethodName(() => registrar.ForCommand<ICommand>(null));
                var forEventMethodCallExpression = Expression.Call(registrarExpression, forCommandMethodName, new[] { messageType }, eventHandlerLambda);

                var registerLambda = Expression.Lambda(forEventMethodCallExpression);
                var registerMethod = (Func<IMessageHandlerRegistrar>)registerLambda.Compile();

                registerMethod();
            }

            return this;
        }

        Action<object> IMessageHandlerRegistry.GetCommandHandler(ICommand message)
        {
            try
            {
                lock(_lock)
                {
                    return _commandHandlers[message.GetType()];
                }
            }
            catch(KeyNotFoundException)
            {
                throw new NoHandlerException(message.GetType());
            }
        }

        Func<IQuery<TResult>, TResult> IMessageHandlerRegistry.GetQueryHandler<TResult>(IQuery<TResult> query)
        {
            try
            {
                lock (_lock)
                {
                    var typeUnsafeQuery = _queryHandlers[query.GetType()];
                    return actualQuery => (TResult)typeUnsafeQuery(actualQuery);
                }
            }
            catch (KeyNotFoundException)
            {
                throw new NoHandlerException(query.GetType());
            }
        }

        IEventDispatcher<IEvent> IMessageHandlerRegistry.CreateEventDispatcher()
        {
            var dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent>();
            var registrar = dispatcher.RegisterHandlers()
                                      .IgnoreUnhandled<IEvent>();
            lock(_lock)
            {
                _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));
            }

            return dispatcher;
        }

        bool IMessageHandlerRegistry.Handles(object aMessage)
        {
            lock(_lock)
            {
                if(aMessage is IEvent)
                    return _eventHandlerRegistrations.Any(registration => registration.Type.IsInstanceOfType(aMessage));

                if(aMessage is ICommand)
                    return _commandHandlers.ContainsKey(aMessage.GetType());

                if(aMessage.GetType().Implements(typeof(IQuery<>)))
                    return _queryHandlers.ContainsKey(aMessage.GetType());
            }

            throw new Exception($"Unhandled message type: {aMessage.GetType()}");
        }

        class EventHandlerRegistration
        {
            public Type Type { get; }
            public Action<IEventHandlerRegistrar<IEvent>> RegisterHandlerWithRegistrar { get; }
            public EventHandlerRegistration(Type type, Action<IEventHandlerRegistrar<IEvent>> registerHandlerWithRegistrar)
            {
                Type = type;
                RegisterHandlerWithRegistrar = registerHandlerWithRegistrar;
            }
        }
    }
}
