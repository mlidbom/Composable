using System;
using Composable.DependencyInjection;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses
{
    public static class MessageHandlerRegistrarWithDependencyInjectionSupportRecommended
    {
        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Action<TCommand, ILocalApiBrowserSession> action) where TCommand : BusApi.ICommand
        {
            @this.Register.ForCommand<TCommand>(command =>  action(command, @this.Resolve<ILocalApiBrowserSession>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResultMine<TCommand, TResult>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Func<TCommand, ILocalApiBrowserSession, TResult> action) where TCommand : BusApi.ICommand<TResult>
        {
            @this.Register.ForCommand<TCommand, TResult>(command =>  action(command, @this.Resolve<ILocalApiBrowserSession>()));
            return @this;
        }

      public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Action<TEvent, ILocalApiBrowserSession> action) where TEvent : BusApi.IEvent
        {
            @this.ForEvent<TEvent>(@event => action(@event, @this.Resolve<ILocalApiBrowserSession>()));
            return @this;
        }

      public static MessageHandlerRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TResult>(this MessageHandlerRegistrarWithDependencyInjectionSupport @this, Func<TQuery, ILocalApiBrowserSession, TResult> action) where TQuery : BusApi.IQuery<TResult>
        {
            @this.Register.ForQuery<TQuery, TResult>(query => action(query, @this.Resolve<ILocalApiBrowserSession>()));
            return @this;
        }
    }
}
