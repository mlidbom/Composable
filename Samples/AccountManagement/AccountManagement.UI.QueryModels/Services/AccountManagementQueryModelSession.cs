using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services
{
    [UsedImplicitly]
    internal class AccountManagementQueryModelSession : DocumentDbSession, IAccountManagementQueryModelSession
    {
        public AccountManagementQueryModelSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
