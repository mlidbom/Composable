using System;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Common
{
    interface IPoolableConnection : IDisposable, IAsyncDisposable
    {
        Task OpenAsyncFlex(AsyncMode syncOrAsync);
    }
}
