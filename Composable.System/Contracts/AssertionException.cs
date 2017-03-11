using System;

namespace Composable.Contracts
{
    class AssertionException : Exception
    {
        public AssertionException(string failureMessage) : base(failureMessage) {}
        public AssertionException() {}
    }
}
