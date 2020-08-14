using System;

namespace Composable.SystemCE
{
    static class ScopedChange
    {
        ///<summary>
        /// <para>A more expressive and concise version of making a change and then using a finally block to ensure that the change is rolled back at some later point.</para>
        /// <para>Executes <paramref name="onEnter"/>immediately and executes <paramref name="onDispose"/> when disposed.</para>
        /// <para>Ensure that you use named parameters to make the call easy to read.</para>
        /// </summary>
        public static IDisposable Enter(Action onEnter, Action onDispose)
        {
            onEnter();
            return DisposableCE.Create(onDispose);
        }
    }
}
