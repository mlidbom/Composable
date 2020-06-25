using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.InMemory.DocumentDB;
using Composable.Persistence.SqlServer.Configuration;
using Composable.Persistence.SqlServer.DocumentDb.SqlServer;
using Composable.Serialization;

// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.Persistence.Common.DependencyInjection
{
    //urgent: Remove persistence layer registration from this class.
    public static class DocumentDbRegistrar
    {
        public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
            => @this.Container.RegisterDocumentDb(@this.Configuration.ConnectionStringName);

        public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IDependencyInjectionContainer @this, string connectionName)
        {
            Contract.Argument(connectionName, nameof(connectionName)).NotNullEmptyOrWhiteSpace();

            //urgent: remove
            if(@this.RunMode.TestingPersistenceLayer == PersistenceLayer.InMemory)
            {
                @this.Register(Singleton.For<IDocumentDb>()
                                         .CreatedBy((IDocumentDbSerializer serializer) => new InMemoryDocumentDb(serializer))
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                @this.Register(Singleton.For<IDocumentDb>()
                                         .CreatedBy((ISqlServerConnectionProviderSource connectionProviderSource, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer)
                                                        => new SqlServerDocumentDb(connectionProviderSource.GetConnectionProvider(connectionName), timeSource, serializer)));
            }


            @this.Register(Scoped.For<IDocumentDbSession, IDocumentDbUpdater, IDocumentDbReader, IDocumentDbBulkReader>()
                                    .CreatedBy((IDocumentDb documentDb) => new DocumentDbSession(documentDb)));

            return new DocumentDbRegistrationBuilder();
        }
    }

    public class DocumentDbRegistrationBuilder
    {
        public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            DocumentDbApi.HandleDocumentType<TDocument>(registrar);
            return this;
        }
    }
}
