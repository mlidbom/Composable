using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE.TasksCE
{
    static partial class TaskCE
    {
        ///<summary>
        /// Abbreviated version of ConfigureAwait(continueOnCapturedContext: false)
        ///We need to apply this to every single await in library code for performance and to avoid deadlocks in clients with a synchronization context. It is not enough to have it at the edges of the public API: https://tinyurl.com/y8cjr77w, https://tinyurl.com/vwrcd8j, https://tinyurl.com/y7sxqb53, https://tinyurl.com/n6zheop,
        ///This is important for performance because when called from a UI thread with a Synchronization context capturing it for every await causes performance problems by a thousand paper cuts: https://tinyurl.com/vwrcd8j
        ///Hacks that null out the SynchronizationContext are not reliable because await uses TaskScheduler.Current so no SetSynchronizationContext hack will work reliably.  https://tinyurl.com/y37d54xg
        ///</summary>
        internal static ConfiguredTaskAwaitable NoMarshalling(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

        ///<summary>
        /// Abbreviated version of ConfigureAwait(continueOnCapturedContext: false)
        ///We need to apply this to every single await in library code for performance and to avoid deadlocks in clients with a synchronization context. It is not enough to have it at the edges of the public API: https://tinyurl.com/y8cjr77w, https://tinyurl.com/vwrcd8j, https://tinyurl.com/y7sxqb53, https://tinyurl.com/n6zheop,
        ///This is important for performance because when called from a UI thread with a Synchronization context capturing it for every await causes performance problems by a thousand paper cuts: https://tinyurl.com/vwrcd8j
        ///Hacks that null out the SynchronizationContext are not reliable because await uses TaskScheduler.Current so no SetSynchronizationContext hack will work reliably.  https://tinyurl.com/y37d54xg
        ///</summary>
        internal static ConfiguredTaskAwaitable<TResult> NoMarshalling<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);
    }
}
