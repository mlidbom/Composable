using System;
using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDb;
using AccountManagement.UI.QueryModels.Generators;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services
{
    [UsedImplicitly]
    internal class AccountManagementQueryModelReader : IAccountManagementQueryModelsReader
    {
        private readonly AccountManagementQueryModelGeneratingDocumentDbReader _generatedModels;
        private readonly IAccountManagementDocumentDbReader _documentDbModels;

        public AccountManagementQueryModelReader(AccountManagementQueryModelGeneratingDocumentDbReader generatedModels, IAccountManagementDocumentDbReader documentDbModels )
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
            if (_documentDbModels.TryGet(accountEmail.ToString(), out accountMap))
            {
                account = GetAccount(accountMap.AccountId);
                return true;
            }
            account = null;
            return false;
        }
    }
}