using System;
using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDbStored;
using AccountManagement.UI.QueryModels.EventStoreGenerated;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services.Implementation
{
    [UsedImplicitly]
    internal class AccountManagementQueryModelReader : IAccountManagementQueryModelsReader
    {
        private readonly IAccountManagementEventStoreGeneratedQueryModelsReader _generatedModels;
        private readonly IAccountManagementDocumentDbQueryModelsReader _documentDbQueryModels;

        public AccountManagementQueryModelReader(IAccountManagementEventStoreGeneratedQueryModelsReader generatedModels, IAccountManagementDocumentDbQueryModelsReader documentDbQueryModels)
        {
            _generatedModels = generatedModels;
            _documentDbQueryModels = documentDbQueryModels;
        }

        public AccountQueryModel GetAccount(Guid accountId)
        {
            return _generatedModels.Get<AccountQueryModel>(accountId);
        }

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
    }
}
