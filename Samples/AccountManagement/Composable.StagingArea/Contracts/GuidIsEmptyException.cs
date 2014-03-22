namespace Composable.Contracts
{
    public class GuidIsEmptyException : ContractException
    {
        public GuidIsEmptyException(string valueName):base(valueName)
        {            
        }
    }
}