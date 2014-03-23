namespace Composable.Contracts
{
    public class GuidIsEmptyException : ContractException
    {
        public GuidIsEmptyException(InspectedValue badValue) : base(badValue) { }
    }
}
