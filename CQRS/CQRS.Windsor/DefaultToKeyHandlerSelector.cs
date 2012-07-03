using System;
using System.Linq;
using Castle.MicroKernel;

namespace Composable.CQRS.Windsor
{
    public class DefaultToKeyHandlerSelector : IHandlerSelector
    {
        private readonly Type _type;
        private readonly string _keyToDefaultTo;

        public DefaultToKeyHandlerSelector(Type type, string keyToDefaultTo)
        {
            _type = type;
            _keyToDefaultTo = keyToDefaultTo;
        }

        public virtual bool HasOpinionAbout(string key, Type service)
        {
            return service == _type;
        }

        public virtual IHandler SelectHandler(string key, Type service, IHandler[] handlers)
        {
            var handlerForDefaultKey = handlers.FirstOrDefault(handler => handler.ComponentModel.Name == _keyToDefaultTo);
            if (handlerForDefaultKey == null)
                return handlers.FirstOrDefault();

            return handlerForDefaultKey;
        }
    }
}