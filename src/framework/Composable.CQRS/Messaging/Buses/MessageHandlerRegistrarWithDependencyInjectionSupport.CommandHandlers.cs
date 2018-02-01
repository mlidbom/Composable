using System;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    public static partial class MessageHandlerRegistrarWithDependencyInjectionSupportExtensions
    {
        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TCommand> action) where TCommand : BusApi.ICommand
        {
            @this.Register.ForCommand(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand, TDependency1>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TCommand, TDependency1> action) where TCommand : BusApi.ICommand
                                                   where TDependency1 : class
        {
            @this.ForCommand<TCommand>(command => action(command, @this.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommand<TCommand, TDependency1, TDependency2>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TCommand, TDependency1, TDependency2> action) where TCommand : BusApi.ICommand
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
        {
            return @this.ForCommand<TCommand, TDependency1>((command, d1) => action(command, d1, @this.Resolve<TDependency2>()));
        }
    }
}
