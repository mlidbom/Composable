using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDb
{
    [UsedImplicitly]
    internal class AccountManagementDocumentDbQueryModelsReader : DocumentDbSession, IAccountManagementDocumentDbReader
    {
        public AccountManagementDocumentDbQueryModelsReader(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
