using System;
using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDbStored;
using AccountManagement.UI.QueryModels.EventStoreGenerated;
using Composable.CQRS.CQRS.Query.Models.Generators;
using Composable.Persistence.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services.Implementation
{
    [UsedImplicitly] class AccountManagementQueryModelReader : IAccountManagementQueryModelsReader
    {
        readonly IAccountManagementDocumentDbQueryModelsReader _documentDbQueryModels;
        readonly QueryModelGeneratingDocumentDbReader _generatedModels;

        public AccountManagementQueryModelReader(IAccountManagementDocumentDbQueryModelsReader documentDbQueryModels,
                                                 AccountQueryModelGenerator accountQueryModelGenerator,
                                                 ISingleContextUseGuard usageGuard)
        {
            _documentDbQueryModels = documentDbQueryModels;
            _generatedModels = new QueryModelGeneratingDocumentDbReader(usageGuard, NullOpDocumentDbSessionInterceptor.Instance, new []{ accountQueryModelGenerator });
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
