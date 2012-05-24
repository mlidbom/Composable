using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.SystemExtensions
{
    public static class ExceptionExtensions
    {
         public static IEnumerable<Exception> GetAllExceptionsInStack(this Exception e)
         {
             do
             {
                 yield return e;
                 e = e.InnerException;
             } while (e != null);
         } 

        public static Exception GetRootCauseException(this Exception e)
        {
            return e.GetAllExceptionsInStack().Last();
        }
    }
}