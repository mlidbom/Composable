using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.UI.QueryModels.Services
{
    internal class AccountManagementQueryModelSession : DocumentDbSession, IAccountManagementQueryModelSession
    {
        public AccountManagementQueryModelSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
