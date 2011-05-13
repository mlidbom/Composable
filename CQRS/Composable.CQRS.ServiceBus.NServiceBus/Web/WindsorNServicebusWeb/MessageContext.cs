using System;
using System.Collections.Generic;

namespace Composable.CQRS.ServiceBus.NServiceBus.Web.WindsorNServicebusWeb
{
    public class MessageContext
    {
        [ThreadStatic] private static MessageContext _current;

        public static MessageContext Current
        {
            get
            {
                if(_current == null)
                {
                    _current = new MessageContext();
                }
                return _current;
            }
        }


        private readonly IDictionary<string, object> _items = new Dictionary<string, object>();


        public object this[string key]
        {
            get
            {
                object item;
                _items.TryGetValue(key, out item);
                return item;
            }
        }


        public void Add(string key, object item)
        {
            _items.Add(key, item);
        }


        public bool Remove(string key)
        {
            return _items.Remove(key);
        }
    }
}