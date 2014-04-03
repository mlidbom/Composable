using AccountManagement.UI.QueryModels.Services;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Readers.Services
{
    [UsedImplicitly]
    internal class AccountManagementDocumentDbQueryModelsReader : DocumentDbSession, IAccountManagementQueryModelsReader, IAccountManagementDocumentDbReader
    {
        public AccountManagementDocumentDbQueryModelsReader(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
