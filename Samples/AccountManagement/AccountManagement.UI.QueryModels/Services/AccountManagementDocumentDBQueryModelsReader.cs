using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Services
{
    [UsedImplicitly]
    internal class AccountManagementDocumentDbQueryModelsReader : DocumentDbSession, IAccountManagementQueryModelsReader
    {
        public AccountManagementDocumentDbQueryModelsReader(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
