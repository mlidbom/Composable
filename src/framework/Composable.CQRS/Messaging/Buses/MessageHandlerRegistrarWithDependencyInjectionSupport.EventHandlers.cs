using System;

namespace Composable.Messaging.Buses
{
    public static partial class MessageHandlerRegistrarWithDependencyInjectionSupportExtensions
    {
        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent> action) where TEvent : BusApi.IEvent
        {
            @this.Register.ForEvent(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1> action) where TEvent : BusApi.IEvent
                                                 where TDependency1 : class
        {
            @this.ForEvent<TEvent>(command => action(command, @this.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2> action) where TEvent : BusApi.IEvent
                                                               where TDependency1 : class
                                                               where TDependency2 : class
        {
            return @this.ForEvent<TEvent, TDependency1>((command, dep1) => action(command, dep1, @this.Resolve<TDependency2>()));
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3> action) where TEvent : BusApi.IEvent
                                                               where TDependency1 : class
                                                               where TDependency2 : class
                                                                             where TDependency3 : class
        {
            return @this.ForEvent<TEvent, TDependency1, TDependency2> ((command, dep1, dep2) => action(command, dep1, dep2, @this.Resolve<TDependency3>()));
        }
    }
}
