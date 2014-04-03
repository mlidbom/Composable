using System;
using System.Collections.Generic;
using AccountManagement.UI.QueryModels.Services;
using Composable.CQRS.Query.Models.Generators;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.UI.QueryModels.EventStore.Generators.Services
{
    public class AccountManagementQueryModelGeneratingQueryModelSession : QueryModelGeneratingDocumentDbReader,  IAccountManagementQueryModelsReader
    {
        public AccountManagementQueryModelGeneratingQueryModelSession(
            ISingleContextUseGuard usageGuard, 
            IDocumentDbSessionInterceptor interceptor,
            IEnumerable<IAccountManagementQueryModelGenerator> documentGenerators) 
            : base(usageGuard, interceptor, documentGenerators) {}

        public AccountQueryModel GetAccount(Guid accountId)
        {
            return Get<AccountQueryModel>(accountId);
        }
    }
}