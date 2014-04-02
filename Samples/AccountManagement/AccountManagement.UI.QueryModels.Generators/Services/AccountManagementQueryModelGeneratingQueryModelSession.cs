using System.Collections.Generic;
using AccountManagement.UI.QueryModels.Services;
using Composable.CQRS.Query.Models.Generators;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.UI.QueryModels.Generators.Services
{
    public class AccountManagementQueryModelGeneratingQueryModelSession : QueryModelGeneratingDocumentDbReader,  IAccountManagementQueryModelsReader
    {
        public AccountManagementQueryModelGeneratingQueryModelSession(
            ISingleContextUseGuard usageGuard, 
            IDocumentDbSessionInterceptor interceptor, 
            IEnumerable<IQueryModelGenerator> documentGenerators) 
            : base(usageGuard, interceptor, documentGenerators) {}
    }
}