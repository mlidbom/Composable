using System;

namespace Composable.Contracts
{
    public class AssertionException : Exception
    {
        public AssertionException(string failureMessage) : base(failureMessage) {}
        public AssertionException() {}
    }
}
