using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.Domain.Services
{
    internal class AccountManagementDomainQueryModelSession : DocumentDbSession, IAccountManagementDomainQueryModelSession
    {
        public AccountManagementDomainQueryModelSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
