﻿using System;
using System.Collections.Generic;

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


        internal static Exception? TryCatch(Action generateException)
        {
            try
            {
                generateException();
            }
            catch(Exception e)
            {
                return e;
            }
            return null;
        }
    }
}