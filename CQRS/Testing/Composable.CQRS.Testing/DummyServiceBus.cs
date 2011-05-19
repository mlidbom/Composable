using System.Collections.Generic;
using Composable.ServiceBus;

namespace Composable.CQRS.Testing
{
    public class DummyServiceBus : IServiceBus
    {
        private readonly IList<object> _published = new List<object>();

        public IEnumerable<object> Published { get { return _published; } }

        public void Reset()
        {
            _published.Clear();
        }

        public void Publish(object message)
        {
            _published.Add(message);
        }
    }
}