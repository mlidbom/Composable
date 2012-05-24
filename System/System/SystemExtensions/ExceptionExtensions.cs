using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.SystemExtensions
{
    public static class ExceptionExtensions
    {
         public static IEnumerable<Exception> GetAllExceptionsInStack(this Exception exception)
         {
             if(exception==null)
             {
                 throw new ArgumentNullException("exception");
             }
             do
             {
                 yield return exception;
                 exception = exception.InnerException;
             } while (exception != null);
         } 

        public static Exception GetRootCauseException(this Exception e)
        {
            return e.GetAllExceptionsInStack().Last();
        }
    }
}