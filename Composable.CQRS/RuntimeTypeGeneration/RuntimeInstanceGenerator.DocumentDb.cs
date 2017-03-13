using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using Composable.Persistence.KeyValueStorage;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.RuntimeTypeGeneration
{
    class DocumentDbFactoryMethods<TSessionInterface,
                                   TUpdaterInterface,
                                   TReaderInterface>
        where TSessionInterface : class, IDocumentDbSession
        where TUpdaterInterface : class, IDocumentDbUpdater
        where TReaderInterface : class, IDocumentDbReader
    {
        readonly Func<object> _internalFactory;
        public DocumentDbFactoryMethods(Func<object> internalFactory) => _internalFactory = internalFactory;

        internal TSessionInterface CreateSession() => (TSessionInterface)_internalFactory();
        internal TReaderInterface CreateReader() => (TReaderInterface)_internalFactory();
        internal TUpdaterInterface CreateUpdater() => (TUpdaterInterface)_internalFactory();
    }

    static partial class RuntimeInstanceGenerator
    {
        internal static class DocumentDb
        {
            static readonly HashSet<Type> ProhibitedBuiltInTypes = Seq.OfTypes<IDocumentDbSession, IDocumentDbReader, IDocumentDbUpdater>()
                                                                      .ToSet();

            internal static DocumentDbFactoryMethods<TSessionInterface, TUpdaterInterface, TReaderInterface> CreateFactoryMethod<
                TSessionInterface,
                TUpdaterInterface,
                TReaderInterface>(
                IWindsorContainer container
                )
                where TSessionInterface : class, IDocumentDbSession
                where TUpdaterInterface : class, IDocumentDbUpdater
                where TReaderInterface : class, IDocumentDbReader
            {

                var requestedServiceInterfaces = Seq.OfTypes<TSessionInterface, TUpdaterInterface, TReaderInterface>()
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
class {subClassName} : 
    {typeof(KeyValueStorage.DocumentDbSession).FullName}, 
    {typeof(TSessionInterface).FullName.Replace("+", ".")},
    {typeof(TUpdaterInterface).FullName.Replace("+", ".")},
    {typeof(TReaderInterface).FullName.Replace("+", ".")}
{{ 
    public {subClassName}(
        {typeof(IDocumentDb).FullName} backingStore, 
        {typeof(ISingleContextUseGuard).FullName} usageGuard, 
        {typeof(IDocumentDbSessionInterceptor).FullName} interceptor)
            :base(backingStore, usageGuard, interceptor)
    {{
    }}
}}";
                var internalFactory = RuntimeInstanceGenerator.CreateFactoryMethod(container, subclassCode, subClassName, requestedServiceInterfaces);
                return new DocumentDbFactoryMethods<TSessionInterface, TUpdaterInterface, TReaderInterface>(internalFactory);
            }
        }
    }
}