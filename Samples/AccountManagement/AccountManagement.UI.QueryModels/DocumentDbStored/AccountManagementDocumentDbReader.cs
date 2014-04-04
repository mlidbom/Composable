using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDbStored
{
    [UsedImplicitly]
    internal class AccountManagementDocumentDbReader : DocumentDbSession, IAccountManagementDocumentDbReader
    {
        public AccountManagementDocumentDbReader(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
