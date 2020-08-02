using System;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Common.AdoCE
{
    interface IPoolableConnection : IDisposable, IAsyncDisposable
    {
        Task OpenAsyncFlex(SyncOrAsync syncOrAsync);
    }
}
