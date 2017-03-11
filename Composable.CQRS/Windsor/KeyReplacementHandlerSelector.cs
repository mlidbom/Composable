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
    sealed class KeyReplacementHandlerSelector : IHandlerSelector
    {
        readonly Type _serviceType;
        readonly string _originalKey;
        readonly string _replacementKey;

        public KeyReplacementHandlerSelector(Type serviceType, string originalKey, string replacementKey)
        {
            _serviceType = serviceType;
            _originalKey = originalKey;
            _replacementKey = replacementKey;
        }

        public bool HasOpinionAbout(string key, Type service)
        {
            return key == _originalKey;
        }

        public IHandler SelectHandler(string key, Type service, IHandler[] handlers)
        {
            if (key == _originalKey)
            {
                var replacementHandler = handlers.FirstOrDefault(handler => handler.ComponentModel.Name == _replacementKey);
                if (replacementHandler != null) return replacementHandler;
            }

            var originalHandler = handlers.FirstOrDefault(handler => handler.ComponentModel.Name == _originalKey);
            if (originalHandler != null) return originalHandler;

            throw new NotSupportedException($"Tried to get key {key} from HandlerSelector for type {_serviceType.Name} but found no matching handler");
        }
    }
}