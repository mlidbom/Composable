using System;

namespace Composable.Messaging.Buses
{
    public static partial class MessageHandlerRegistrarWithDependencyInjectionSupportExtensions
    {
        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TQuery, TResult> action) where TQuery : BusApi.IQuery<TResult>
        {
            @this.Register.ForQuery(action);
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TQuery, TDependency1, TResult> action) where TQuery : BusApi.IQuery<TResult>
                                                        where TDependency1 : class
        {
            @this.Register.ForQuery<TQuery, TResult>(query => action(query, @this.Resolve<TDependency1>()));
            return @this;
        }

        public static MessageHandlerRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TResult>(
            this MessageHandlerRegistrarWithDependencyInjectionSupport @this,
            Func<TQuery, TDependency1, TDependency2, TResult> action) where TQuery : BusApi.IQuery<TResult>
                                                        where TDependency1 : class
                                                        where TDependency2 : class
        {
            return @this.ForQuery<TQuery, TDependency1, TResult>((query, d1) => action(query, d1, @this.Resolve<TDependency2>()));
        }
    }
}
