using System;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Testing
{
    public static class AssertThrows
    {
        public static async Task<TException> Async<TException>([JetBrains.Annotations.InstantHandle]Func<Task> action) where TException : Exception
        {
         try
         {
             await action().NoMarshalling();
         }
         catch(TException exception)
         {
             return exception;
         }
         catch(Exception anyException)
         {
             throw new Exception($"Expected exception of type: {typeof(TException)}, but thrown exception is: {anyException.GetType()}", anyException);
         }

         throw new Exception($"Expected exception of type: {typeof(TException)}, but no exception was thrown");
        }

        public static TException Exception<TException>([JetBrains.Annotations.InstantHandle]Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch(TException exception)
            {
                return exception;
            }
            catch(Exception anyException)
            {
                throw new Exception($"Expected exception of type: {typeof(TException)}, but thrown exception is: {anyException.GetType()}", anyException);
            }

            throw new Exception($"Expected exception of type: {typeof(TException)}, but no exception was thrown");
        }
    }
}
