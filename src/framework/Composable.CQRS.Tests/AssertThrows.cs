using System;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests
{
    static class AssertThrows
    {
        internal static TException Exception<TException>([InstantHandle]Func<object> action) where TException : Exception
        {
            return Exception<TException>(() =>
            {
                action();
            });
        }

        internal static TException Exception<TException>([InstantHandle]Action action) where TException : Exception
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
                throw new AssertionException($"Expected exception of type: {typeof(TException)}, but thrown exception is: {anyException.GetType()}", anyException);
            }

            throw new AssertionException($"Expected exception of type: {typeof(TException)}, but no exception was thrown");
        }
    }
}
