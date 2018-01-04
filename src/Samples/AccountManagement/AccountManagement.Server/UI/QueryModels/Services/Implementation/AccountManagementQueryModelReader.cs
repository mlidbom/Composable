using System;
using Composable.Persistence.EventStore.Query.Models.Generators;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services.Implementation
{
    [UsedImplicitly] class AccountManagementQueryModelReader
    {
        readonly QueryModelGeneratingDocumentDbReader _generatedModels;

        public AccountManagementQueryModelReader(IAccountManagementUiDocumentDbReader documentDbQueryModels,
                                                 AccountQueryModel.Generator accountQueryModelGenerator,
                                                 ISingleContextUseGuard usageGuard) => _generatedModels = new QueryModelGeneratingDocumentDbReader(usageGuard, new []{ accountQueryModelGenerator });

        public AccountQueryModel GetAccount(Guid accountId, int version) => _generatedModels.GetVersion<AccountQueryModel>(accountId, version);
    }
}
