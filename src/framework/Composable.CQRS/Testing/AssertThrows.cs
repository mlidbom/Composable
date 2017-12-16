using System;
using JetBrains.Annotations;

namespace Composable.Testing
{
    public static class AssertThrows
    {
        public static TException Exception<TException>([InstantHandle]Func<object> action) where TException : Exception
        {
            return Exception<TException>(() =>
            {
                action();
            });
        }

        public static TException Exception<TException>([InstantHandle]Action action) where TException : Exception
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
