using System;

namespace Composable.Contracts
{
    public class ContractException : Exception
    {
        protected ContractException(string valueName):base(message:valueName)
        {
            
        }
    }
}