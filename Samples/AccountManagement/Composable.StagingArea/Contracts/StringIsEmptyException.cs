namespace Composable.Contracts
{
    public class StringIsEmptyException : ContractException
    {
        public StringIsEmptyException(string valueName) : base(valueName) {}
    }
}
