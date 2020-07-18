using System;
using System.Threading.Tasks;
using Composable.System.Threading;

namespace Composable.Logging
{
    static class ExceptionLogger
    {
        internal static void ExceptionsAndRethrow(this ILogger log, Action action)
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        internal static async Task ExceptionsAndRethrowAsync(this ILogger log, Func<Task> action)
        {
            try
            {
                await action().NoMarshalling();
            }
            catch(Exception e)
            {
                log.Error(e);
                throw;
            }
        }
    }
}
