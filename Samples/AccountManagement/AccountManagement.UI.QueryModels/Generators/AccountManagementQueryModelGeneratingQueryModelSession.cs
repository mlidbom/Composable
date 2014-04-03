using System;
using System.Collections.Generic;
using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDb;
using AccountManagement.UI.QueryModels.Services;
using Composable.CQRS.Query.Models.Generators;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.UI.QueryModels.Generators
{
    public class AccountManagementQueryModelGeneratingQueryModelSession : QueryModelGeneratingDocumentDbReader,  IAccountManagementQueryModelsReader
    {
        private readonly IAccountManagementDocumentDbReader _documentDbReader;

        public AccountManagementQueryModelGeneratingQueryModelSession(
            ISingleContextUseGuard usageGuard, 
            IDocumentDbSessionInterceptor interceptor,
            IEnumerable<IAccountManagementQueryModelGenerator> documentGenerators,
            IAccountManagementDocumentDbReader documentDbReader) 
            : base(usageGuard, interceptor, documentGenerators)
        {
            _documentDbReader = documentDbReader;
        }

        public AccountQueryModel GetAccount(Guid accountId)
        {
            return Get<AccountQueryModel>(accountId);
        }

        public bool TryGetAccountByEmail(Email accountEmail, out AccountQueryModel account)
        {
            EmailToAccountMapQueryModel accountMap;
            if(_documentDbReader.TryGet(accountEmail.ToString(), out accountMap))
            {
                account = GetAccount(accountMap.AccountId);
                return true;
            }
            account = null;
            return false;
        }
    }
}