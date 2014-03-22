namespace Composable.Contracts
{
    public class StringIsWhitespaceException : ContractException
    {
        public StringIsWhitespaceException(string valueName) : base(valueName) {}
    }
}
