using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Composable.System.Threading
{
    static class TaskExtensions
    {
        public static ConfiguredTaskAwaitable NoMarshalling(this Task @this) => @this.ConfigureAwait(continueOnCapturedContext: false);

        public static ConfiguredTaskAwaitable<TResult> NoMarshalling<TResult>(this Task<TResult> @this) => @this.ConfigureAwait(continueOnCapturedContext: false);
    }
}
