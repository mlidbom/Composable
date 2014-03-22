namespace Composable.Contracts.Tests
{
    public class ObjectIsNullException : ContractException {
        public ObjectIsNullException(string valueName) : base(valueName) {}
    }
}