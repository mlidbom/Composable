using System.Threading;

namespace Composable.SystemCE.ThreadingCE
{
    static class ThreadCE
    {
        public static void InterruptAndJoin(this Thread @this)
        {
            @this.Interrupt();
            @this.Join();
        }
    }
}
