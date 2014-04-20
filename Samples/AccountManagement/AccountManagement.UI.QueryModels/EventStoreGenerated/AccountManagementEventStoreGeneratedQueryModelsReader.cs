using System.Collections.Generic;
using Composable.CQRS.Query.Models.Generators;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.UI.QueryModels.EventStoreGenerated
{
    public class AccountManagementEventStoreGeneratedQueryModelsReader : QueryModelGeneratingDocumentDbReader, IAccountManagementEventStoreGeneratedQueryModelsReader
    {
        public AccountManagementEventStoreGeneratedQueryModelsReader(
            ISingleContextUseGuard usageGuard,
            IDocumentDbSessionInterceptor interceptor,
            IEnumerable<IAccountManagementQueryModelGenerator> documentGenerators)
            : base(usageGuard, interceptor, documentGenerators) {}
    }
}
