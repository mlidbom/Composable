using System;

namespace Composable.SystemCE
{
    static class ScopedChange
    {
        ///<summary>Executes <param name="enterAction"> immediately and executes <param name="exitAction"> when disposed. A for more expressive and concise version of making a change and then using a finally block to ensure that the change is rolled back.</param></param></summary>
        public static IDisposable Enter(Action enterAction, Action exitAction)
        {
            enterAction();
            return DisposableCE.Create(exitAction);
        }
    }
}
