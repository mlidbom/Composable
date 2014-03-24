namespace Composable.Contracts
{
    public class StringIsEmptyException : ContractException
    {
        public StringIsEmptyException(InspectedValue badValue) : base(badValue) {}
    }
}
