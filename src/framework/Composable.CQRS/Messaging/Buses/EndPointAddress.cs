using Composable.Contracts;
using Composable.DDD;

namespace Composable.Messaging.Buses
{
    public class EndPointAddress : ValueObject<EndPointAddress>
    {
        internal string StringValue { get; private set; }
        internal EndPointAddress(string stringValue)
        {
            Contract.Argument(() => stringValue).NotNullEmptyOrWhiteSpace();
            StringValue = stringValue;
        }
    }
}
