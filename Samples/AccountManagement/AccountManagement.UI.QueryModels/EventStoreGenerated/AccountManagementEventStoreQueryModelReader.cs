using System.Collections.Generic;
using Composable.CQRS.CQRS.Query.Models.Generators;
using Composable.Persistence.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.EventStoreGenerated
{
    [UsedImplicitly]
    class AccountManagementEventStoreQueryModelReader : QueryModelGeneratingDocumentDbReader//, IAccountManagementQueryModelGeneratingDocumentDbReader
    {
        public AccountManagementEventStoreQueryModelReader(
            ISingleContextUseGuard usageGuard,
            IDocumentDbSessionInterceptor interceptor,
            IEnumerable<IAccountManagementQueryModelGenerator> documentGenerators)
            : base(usageGuard, interceptor, documentGenerators) {}
    }
}
