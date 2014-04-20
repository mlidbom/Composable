using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDbStored
{
    [UsedImplicitly]
    internal class AccountManagementDocumentDbQueryModelsReader : DocumentDbSession, IAccountManagementDocumentDbQueryModelsReader
    {
        public AccountManagementDocumentDbQueryModelsReader(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
