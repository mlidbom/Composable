using System;

namespace Composable.Contracts.Tests
{
    public static class ReturnValueContractHelper
    {
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return Contract.Return(returnValue, assert);
        }
    }
}