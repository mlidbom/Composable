namespace Composable.Contracts
{
    public class ObjectIsNullException : ContractException
    {
        public ObjectIsNullException(string valueName) : base(valueName) {}
    }
}
