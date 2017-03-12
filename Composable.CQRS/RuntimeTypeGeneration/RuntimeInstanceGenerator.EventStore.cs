using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.KeyValueStorage;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.RuntimeTypeGeneration
{
    class EventStoreFactoryMethods<TSessionInterface, TReaderInterface>
        where TSessionInterface : IEventStoreSession
        where TReaderInterface : IEventStoreReader
    {
        readonly Func<IWindsorContainer, object> _internalFactory;
        public EventStoreFactoryMethods(Func<IWindsorContainer, object> internalFactory) => _internalFactory = internalFactory;

        internal TSessionInterface CreateSession(IWindsorContainer container) => (TSessionInterface)_internalFactory(container);
        internal TReaderInterface CreateReader(IWindsorContainer container) => (TReaderInterface)_internalFactory(container);
    }

    static partial class RuntimeInstanceGenerator
    {
        internal static class EventStore
        {
            static readonly HashSet<Type> ProhibitedBuiltInTypes = Seq.OfTypes<IEventStoreSession, IEventStoreReader>()
                                                                      .ToSet();

            internal static EventStoreFactoryMethods<TSessionInterface, TReaderInterface> CreateFactoryMethod<
                TSessionInterface, TReaderInterface>()
                where TSessionInterface : IEventStoreSession
                where TReaderInterface : IEventStoreReader
            {

                var requestedServiceInterfaces = Seq.OfTypes<TSessionInterface, TReaderInterface>()
                                                    .ToArray();

                if(requestedServiceInterfaces.ToSet()
                                             .Intersect(ProhibitedBuiltInTypes)
                                             .Any())
                {
                    throw new ArgumentException("The service interfaces you supply must inherit from the built in interfaces. You are not allowed to supply the built in interfaces.");
                }

                var subClassName = SubClassName<TSessionInterface>();
                string subclassCode =
                    $@"
public class {subClassName} : 
    {typeof(EventStoreSession).FullName}, 
    {typeof(TSessionInterface).FullName.Replace("+", ".")},
    {typeof(TReaderInterface).FullName.Replace("+", ".")}
{{ 
    public {subClassName}(
        {typeof(IServiceBus).FullName} serviceBus, 
        {typeof(IEventStore).FullName} eventStore, 
        {typeof(ISingleContextUseGuard).FullName} usageGuard,
        {typeof(IUtcTimeTimeSource).FullName} timeSource)
            :base(serviceBus, eventStore, usageGuard, timeSource)
    {{
    }}
}}";
                var internalFactory = RuntimeInstanceGenerator.CreateFactoryMethod(subclassCode, subClassName, requestedServiceInterfaces);
                return new EventStoreFactoryMethods<TSessionInterface, TReaderInterface>(internalFactory);
            }
        }
    }
}