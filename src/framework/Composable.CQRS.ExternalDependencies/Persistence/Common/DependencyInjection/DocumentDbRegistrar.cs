using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using Composable.Refactoring.Naming;
using Composable.Serialization;

namespace Composable.Persistence.Common.DependencyInjection
{
    public static class DocumentDbRegistrar
    {
        public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
            => @this.Container.RegisterDocumentDb(@this.Configuration.ConnectionStringName);

        public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IDependencyInjectionContainer @this, string connectionName)
        {
            Contract.ArgumentNotNullEmptyOrWhitespace(connectionName, nameof(connectionName));

            @this.Register(Singleton.For<IDocumentDbSerializer>()
                                    .CreatedBy((ITypeMapper typeMapper) => new DocumentDbSerializer(typeMapper)));

            @this.Register(Scoped.For<IDocumentDb>()
                                     .CreatedBy((IDocumentDbPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer)
                                                    => new DocumentDb.DocumentDb(timeSource, serializer, typeMapper, persistenceLayer)));

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
