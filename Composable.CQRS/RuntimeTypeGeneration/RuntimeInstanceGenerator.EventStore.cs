using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.RuntimeTypeGeneration
{
    static partial class RuntimeInstanceGenerator
    {
        internal static class EventStore
        {
            static readonly HashSet<Type> ProhibitedBuiltInTypes = Seq.OfTypes<IEventStoreSession, IEventStoreReader>()
                                                                      .ToSet();

            internal static Type CreateType<
                TSessionInterface, TReaderInterface>()
                where TSessionInterface : IEventStoreSession
                where TReaderInterface : IEventStoreReader
            {
                GenerateCode<TSessionInterface, TReaderInterface>(out string subClassName, out string subclassCode);
                return InternalGenerate(subclassCode, subClassName, typeof(TSessionInterface).Assembly);
            }

            static void GenerateCode<TSessionInterface, TReaderInterface>(out string subClassName, out string subclassCode)
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

                subClassName = SubClassName<TSessionInterface>();
                subclassCode = $@"
class {subClassName} : 
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
            }
        }
    }
}
