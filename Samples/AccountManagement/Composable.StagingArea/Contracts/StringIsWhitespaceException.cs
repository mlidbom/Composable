namespace Composable.Contracts
{
    public class StringIsWhitespaceException : ContractException
    {
        public StringIsWhitespaceException(InspectedValue badValue) : base(badValue) { }
    }
}
