using System;
using System.Threading;

namespace Composable.Tests.Testing
{
    public static class WaitHandleTestingExtensions
    {
        public static void AssertWaitOneDoesNotTimeout(this WaitHandle @this, TimeSpan timeout)
        {
            if(!@this.WaitOne(timeout))
            {
                throw new Exception("Timed out waiting");
            }
        }
    }
}
