using System;
using AccountManagement.Domain;
using AccountManagement.UI.QueryModels.EventStoreGenerated;
using Composable.Persistence.EventStore.Query.Models.Generators;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services.Implementation
{
    [UsedImplicitly] class AccountManagementQueryModelReader : IAccountManagementQueryModelsReader
    {
        readonly IAccountManagementUiDocumentDbReader _documentDbQueryModels;
        readonly QueryModelGeneratingDocumentDbReader _generatedModels;

        public AccountManagementQueryModelReader(IAccountManagementUiDocumentDbReader documentDbQueryModels,
                                                 AccountQueryModelGenerator accountQueryModelGenerator,
                                                 ISingleContextUseGuard usageGuard)
        {
            _documentDbQueryModels = documentDbQueryModels;
            _generatedModels = new QueryModelGeneratingDocumentDbReader(usageGuard, new []{ accountQueryModelGenerator });
        }

        public AccountQueryModel GetAccount(Guid accountId) => _generatedModels.Get<AccountQueryModel>(accountId);

        public bool TryGetAccountByEmail(Email accountEmail, out AccountQueryModel account)
        {
            EmailToAccountMapQueryModel accountMap;
            if(_documentDbQueryModels.TryGet(accountEmail.ToString(), out accountMap))
            {
                account = GetAccount(accountMap.AccountId);
                return true;
            }
            account = null;
            return false;
        }

        public AccountQueryModel GetAccount(Guid accountId, int version) => _generatedModels.GetVersion<AccountQueryModel>(accountId, version);
    }
}
