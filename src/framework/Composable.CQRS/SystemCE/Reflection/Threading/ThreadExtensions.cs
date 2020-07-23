using System.Threading;

namespace Composable.SystemCE.Reflection.Threading
{
    static class ThreadExtensions
    {
        public static void InterruptAndJoin(this Thread @this)
        {
            @this.Interrupt();
            @this.Join();
        }
    }
}
