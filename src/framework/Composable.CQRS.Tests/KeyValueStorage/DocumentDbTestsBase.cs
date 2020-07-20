using System;
using Composable.DependencyInjection;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;
using NUnit.Framework;
using NCrunch.Framework;

namespace Composable.Tests.KeyValueStorage
{
    //urgent: Remove this attribute once whole assembly runs all persistence layers.
    [DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.Memory), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.PgSql), nameof(PersistenceLayer.Orcl), nameof(PersistenceLayer.DB2))]
    class DocumentDbTestsBase
    {
        protected IDocumentDb CreateStore() => TestWiringHelper.DocumentDb(ServiceLocator);
        protected IServiceLocator ServiceLocator { get; private set; }
        protected IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(
            builder
                => builder.TypeMapper
                          .Map<Composable.Tests.KeyValueStorage.User>("96f37428-04ca-4f60-858a-785d26ee7576")
                          .Map<Composable.Tests.KeyValueStorage.Email>("648191d9-bfae-45c0-b824-d322d01fa64c")
                          .Map<Composable.Tests.KeyValueStorage.Dog>("ca527ca3-d352-4674-9133-2747756f45b3")
                          .Map<Composable.Tests.KeyValueStorage.Person>("64133a9b-1279-4029-9469-2d63d4f9ceaa")
                          .Map<global::System.Collections.Generic.HashSet<Composable.Tests.KeyValueStorage.User>>("df57e323-d4b0-44c1-a69c-5ea100af9ebf"));
        [SetUp]
        public void Setup()
        {
            ServiceLocator = CreateServiceLocator();
        }
        [TearDown]
        public void TearDownTask()
        {
            ServiceLocator?.Dispose();
        }
        protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession)
        {
            ServiceLocator.ExecuteTransactionInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));
        }
        internal void UseInScope([InstantHandle]Action<IDocumentDbReader> useSession)
        {
            ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
        }
    }
}
