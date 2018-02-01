using System;

namespace Composable.Messaging.Buses
{
    public static partial class MessageHandlerRegistrarWithDependencyInjectionSupportExtensions
    {
        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResult<TCommand, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TCommand, TResult> action) where TCommand : BusApi.ICommand<TResult>
        {
            @this.Register.ForCommand(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResult<TCommand, TDependency1, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TCommand, TDependency1, TResult> action) where TCommand : BusApi.ICommand<TResult>
                                                          where TDependency1 : class
        {
            @this.Register.ForCommand<TCommand, TResult>(command => action(command, @this.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForCommandWithResult<TCommand, TDependency1, TDependency2, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TCommand, TDependency1, TDependency2, TResult> action) where TCommand : BusApi.ICommand<TResult>
                                                                        where TDependency1 : class
                                                                        where TDependency2 : class
        {
            @this.Register.ForCommand<TCommand, TResult>(command => action(command, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>()));
            return @this;
        }
    }
}
