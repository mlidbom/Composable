using System;

namespace Composable.Contracts
{
    public class ContractException : Exception
    {
        public ContractException(string valueName) : base(message: valueName) {}
    }
}
