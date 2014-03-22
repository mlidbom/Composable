namespace Composable.Contracts
{
    public class ObjectIsDefaultException : ContractException
    {
        public ObjectIsDefaultException(string valueName) : base(valueName) { }
    }
}