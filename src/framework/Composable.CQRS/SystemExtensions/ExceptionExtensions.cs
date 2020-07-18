using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Contracts;

namespace Composable.SystemExtensions
{
    ///<summary>Extensions for working with extensions</summary>
    static class ExceptionExtensions
    {
        ///<summary>Flattens the exception.InnerException hierarchy into a sequence.</summary>
         public static IEnumerable<Exception> GetAllExceptionsInStack(this Exception exception)
        {
            Contract.Argument(exception, nameof(exception))
                             .NotNull();

             do
             {
                 yield return exception;
                 exception = exception.InnerException;
             } while (exception != null);
        }

        ///<summary>Returns the deepest nested inner exception that was the root cause of the current exception.</summary>
        public static Exception GetRootCauseException(this Exception e) => e.GetAllExceptionsInStack().Last();


        internal static Exception? TryCatch(Action action)
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                return e;
            }
            return null;
        }

        internal static bool TryCatch(Action action, [NotNullWhen(true)]out Exception? exception)
        {
            try
            {
                action();
            }
            catch(Exception caught)
            {
                exception = caught;
                return true;
            }

            exception = null;
            return false;
        }

        internal static void TryCatch(Action action, Action<Exception> onException)
        {
            try
            {
                action();
            }
            catch(Exception caught)
            {
                onException(caught);
            }
        }
    }
}