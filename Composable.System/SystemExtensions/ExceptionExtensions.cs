using System;
using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;

namespace Composable.SystemExtensions
{
    ///<summary>Extensions for working with extensions</summary>
    public static class ExceptionExtensions
    {
        ///<summary>Flattens the exception.InnerException hierarchy into a sequence.</summary>
         public static IEnumerable<Exception> GetAllExceptionsInStack(this Exception exception)
        {
            ContractOptimized.Argument(exception, nameof(exception))
                             .NotNull();

             do
             {
                 yield return exception;
                 exception = exception.InnerException;
             } while (exception != null);
         }

        ///<summary>Returns the deepest nested inner exception that was the root cause of the current exception.</summary>
        public static Exception GetRootCauseException(this Exception e)
        {
            return e.GetAllExceptionsInStack().Last();
        }
    }
}