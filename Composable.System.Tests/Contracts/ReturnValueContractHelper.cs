using System;
using Composable.Contracts;

namespace Composable.Tests.Contracts
{
    static class ReturnValueContractHelper
    {
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return Contract.Return(returnValue, assert);
        }
    }
}
