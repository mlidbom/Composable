using System;
using Composable.Contracts;

namespace Composable.Tests.Contracts
{
    static class ReturnValueContractHelper
    {
        public static void Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            Contract.Return(returnValue, assert);
        }
    }
}
