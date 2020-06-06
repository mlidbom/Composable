using System;

namespace Composable.Messaging.Buses
{
    public static partial class MessageHandlerRegistrarWithDependencyInjectionSupportExtensions
    {
        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent> handler) where TEvent : MessageTypes.IEvent
        {
            @this.Register.ForEvent(handler);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1> handler) where TEvent : MessageTypes.IEvent
                                                  where TDependency1 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2> handler) where TEvent : MessageTypes.IEvent
                                                                where TDependency1 : class
                                                                where TDependency2 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3> handler) where TEvent : MessageTypes.IEvent
                                                                              where TDependency1 : class
                                                                              where TDependency2 : class
                                                                              where TDependency3 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3, TDependency4>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3, TDependency4> handler) where TEvent : MessageTypes.IEvent
                                                                                            where TDependency1 : class
                                                                                            where TDependency2 : class
                                                                                            where TDependency3 : class
                                                                                            where TDependency4 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5> handler) where TEvent : MessageTypes.IEvent
                                                                                                          where TDependency1 : class
                                                                                                          where TDependency2 : class
                                                                                                          where TDependency3 : class
                                                                                                          where TDependency4 : class
                                                                                                          where TDependency5 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6> handler) where TEvent : MessageTypes.IEvent
                                                                                                                        where TDependency1 : class
                                                                                                                        where TDependency2 : class
                                                                                                                        where TDependency3 : class
                                                                                                                        where TDependency4 : class
                                                                                                                        where TDependency5 : class
                                                                                                                        where TDependency6 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7> handler) where TEvent : MessageTypes.IEvent
                                                                                                                                      where TDependency1 : class
                                                                                                                                      where TDependency2 : class
                                                                                                                                      where TDependency3 : class
                                                                                                                                      where TDependency4 : class
                                                                                                                                      where TDependency5 : class
                                                                                                                                      where TDependency6 : class
                                                                                                                                      where TDependency7 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8> handler) where TEvent : MessageTypes.IEvent
                                                                                                                                                    where TDependency1 : class
                                                                                                                                                    where TDependency2 : class
                                                                                                                                                    where TDependency3 : class
                                                                                                                                                    where TDependency4 : class
                                                                                                                                                    where TDependency5 : class
                                                                                                                                                    where TDependency6 : class
                                                                                                                                                    where TDependency7 : class
                                                                                                                                                    where TDependency8 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>(), @this.Resolve<TDependency8>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForEvent<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Action<TEvent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9> handler) where TEvent : MessageTypes.IEvent
                                                                                                                                                                  where TDependency1 : class
                                                                                                                                                                  where TDependency2 : class
                                                                                                                                                                  where TDependency3 : class
                                                                                                                                                                  where TDependency4 : class
                                                                                                                                                                  where TDependency5 : class
                                                                                                                                                                  where TDependency6 : class
                                                                                                                                                                  where TDependency7 : class
                                                                                                                                                                  where TDependency8 : class
                                                                                                                                                                  where TDependency9 : class
        {
            @this.Register.ForEvent<TEvent>(@event => handler(@event, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>(), @this.Resolve<TDependency8>(), @this.Resolve<TDependency9>()));
            return @this;
        }
    }
}
