using System;
using System.Linq;
using Castle.MicroKernel;

namespace Composable.CQRS.Windsor
{
    /// <summary>
    /// When multiple registrations to the same type are made this HandlerSelector returns the service registered with the 'replacementKey'
    /// when the container is asked for a service with 'originalKey'
    /// Use it by adding it to the container at wire-up with container.Kernel.AddHandlerSelector(new KeyReplacementHandlerSelector(typeof([ComponentType]),"originalKey", "replacementKey"));
    /// </summary>
    public class KeyReplacementHandlerSelector : IHandlerSelector
    {
        private readonly Type _serviceType;
        private readonly string _originalKey;
        private readonly string _replacementKey;

        public KeyReplacementHandlerSelector(Type serviceType, string originalKey, string replacementKey)
        {
            _serviceType = serviceType;
            _originalKey = originalKey;
            _replacementKey = replacementKey;
        }

        public virtual bool HasOpinionAbout(string key, Type service)
        {
            return key == _originalKey;
        }

        public virtual IHandler SelectHandler(string key, Type service, IHandler[] handlers)
        {
            if (key == _originalKey)
            {
                var replacementHandler = handlers.FirstOrDefault(handler => handler.ComponentModel.Name == _replacementKey);
                if (replacementHandler != null) return replacementHandler;
            }

            var originalHandler = handlers.FirstOrDefault(handler => handler.ComponentModel.Name == _originalKey);
            if (originalHandler != null) return originalHandler;

            throw new NotSupportedException(string.Format("Tried to get key {0} from HandlerSelector for type {1} but found no matching handler", key, _serviceType.Name));
        }
    }
}