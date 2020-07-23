using System.Threading;

namespace Composable.SystemCE.ThreadingCE
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
