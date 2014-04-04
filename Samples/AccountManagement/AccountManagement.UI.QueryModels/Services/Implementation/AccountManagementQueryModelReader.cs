using System;
using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDb;
using AccountManagement.UI.QueryModels.DocumentDbStored;
using AccountManagement.UI.QueryModels.EventStoreGenerated;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services.Implementation
{
    [UsedImplicitly]
    internal class AccountManagementQueryModelReader : IAccountManagementQueryModelsReader
    {
        private readonly IAccountManagementQueryModelGeneratingDocumentDbReader _generatedModels;
        private readonly IAccountManagementDocumentDbReader _documentDbModels;

        public AccountManagementQueryModelReader(IAccountManagementQueryModelGeneratingDocumentDbReader generatedModels, IAccountManagementDocumentDbReader documentDbModels)
        {
            _generatedModels = generatedModels;
            _documentDbModels = documentDbModels;
        }

        public AccountQueryModel GetAccount(Guid accountId)
        {
            return _generatedModels.Get<AccountQueryModel>(accountId);
        }

        public bool TryGetAccountByEmail(Email accountEmail, out AccountQueryModel account)
        {
            EmailToAccountMapQueryModel accountMap;
            if(_documentDbModels.TryGet(accountEmail.ToString(), out accountMap))
            {
                account = GetAccount(accountMap.AccountId);
                return true;
            }
            account = null;
            return false;
        }
    }
}
