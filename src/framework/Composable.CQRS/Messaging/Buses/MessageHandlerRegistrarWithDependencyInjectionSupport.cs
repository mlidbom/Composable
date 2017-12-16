using System;
using Composable.DependencyInjection;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses
{
    public class MessageHandlerRegistrarWithDependencyInjectionSupport
    {
        public MessageHandlerRegistrarWithDependencyInjectionSupport(IMessageHandlerRegistrar register, IServiceLocator serviceLocator)
        {
            Register = register;
            ServiceLocator = serviceLocator;
        }

        internal IMessageHandlerRegistrar Register { get; }

        internal IServiceLocator ServiceLocator { get; }
    }

    public static class MessageHandlerRegistrarWithDependencyInjectionSupportExtensions
    {

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TCommand> action) where TCommand : IDomainCommand
        {
            @this.Register.ForCommand(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResult<TCommand, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TCommand, TResult> action) where TCommand : IDomainCommand<TResult>
        {
            @this.Register.ForCommand(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResult<TCommand, TDependency1, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TCommand, TDependency1, TResult> action) where TCommand : IDomainCommand<TResult>
                                                          where TDependency1 : class
        {
            @this.Register.ForCommand<TCommand,TResult>(command => action(command, @this.ServiceLocator.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResult<TCommand, TDependency1, TDependency2, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TCommand, TDependency1, TDependency2, TResult> action) where TCommand : IDomainCommand<TResult>
                                                          where TDependency1 : class
                                                          where TDependency2 : class
        {
            return @this.ForCommandWithResult<TCommand, TDependency1, TResult>((command, d1)=> action(command, d1, @this.ServiceLocator.Resolve<TDependency2>()));
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand, TDependency1>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TCommand, TDependency1> action) where TCommand : IDomainCommand
                                                   where TDependency1 : class
        {
            if(typeof(TCommand).Implements(typeof(IDomainCommand<>)))
            {
                throw new Exception($"{typeof(TCommand)} expects a result. You must register a method that returns a result.");
            }

            @this.ForCommand<TCommand>(command => action(command, @this.ServiceLocator.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand, TDependency1, TDependency2>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TCommand, TDependency1, TDependency2> action) where TCommand : IDomainCommand
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
        {
            return @this.ForCommand<TCommand, TDependency1>((command, d1) => action(command, d1, @this.ServiceLocator.Resolve<TDependency2>()));
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent> action) where TEvent : IEvent
        {
            @this.Register.ForEvent(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1> action) where TEvent : IEvent
                                                 where TDependency1 : class
        {
            @this.ForEvent<TEvent>(command => action(command, @this.ServiceLocator.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TQuery, TResult> action) where TQuery : IQuery<TResult>
        {
            @this.Register.ForQuery(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TQuery, TDependency1, TResult> action) where TQuery : IQuery<TResult>
                                                        where TDependency1 : class
        {
            @this.Register.ForQuery<TQuery,TResult>(query =>  action(query, @this.ServiceLocator.Resolve<TDependency1>()));
            return @this;
        }
    }
}
