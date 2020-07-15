using System;
using System.Threading.Tasks;

namespace Composable.Logging
{
    static class ExceptionLogger
    {
        internal static void Exceptions(this ILogger log, Action action)
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

        internal static async Task ExceptionsAsync(this ILogger log, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch(Exception e)
            {
                log.Error(e);
                throw;
            }
        }
    }
}
