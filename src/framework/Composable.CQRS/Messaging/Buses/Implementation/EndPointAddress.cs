using Composable.Contracts;
using Composable.DDD;

namespace Composable.Messaging.Buses.Implementation
{
    public class EndPointAddress : ValueObject<EndPointAddress>
    {
        internal string StringValue { get; private set; }
        internal EndPointAddress(string stringValue)
        {
            Contract.ArgumentNotNullEmptyOrWhitespace(stringValue, nameof(stringValue));
            StringValue = stringValue;
        }
    }
}
