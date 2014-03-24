using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Updaters.Services
{
    [UsedImplicitly]
    public class AccountManagementQueryModelUpdaterSession : DocumentDbSession, IAccountManagementQueryModelUpdaterSession
    {
        public AccountManagementQueryModelUpdaterSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
