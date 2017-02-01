using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class AccountManagementDomainQueryModelSession : DocumentDbSession, IAccountManagementDomainQueryModelSession
    {
        public AccountManagementDomainQueryModelSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
