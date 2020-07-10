using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator
    {
        IServiceLocator? _createdServiceLocator;
        readonly Dictionary<Guid, ComponentRegistration> _registeredComponents = new Dictionary<Guid, ComponentRegistration>();
        bool _disposed;

        public IRunMode RunMode { get; }

        public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents.Values.ToList();

        internal ComposableDependencyInjectionContainer(IRunMode runMode) => RunMode = runMode;

        public void Register(params ComponentRegistration[] registrations)
        {
            Assert.State.Assert(_createdServiceLocator == null);

            foreach(var registration in registrations)
            {
                _registeredComponents.Add(registration.Id, registration);
            }
        }

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator()
        {
            Assert.State.Assert(!_disposed);
            if(_createdServiceLocator == null)
            {
                _createdServiceLocator = new ServiceLocator(_registeredComponents.Values.ToList());
                //This shows as a hotspot for the tests when profiling. But does not negatively effect total test suite execution time according to my tests.
                Verify();
            }

            return this;
        }

        void Verify()
        {
            using(_createdServiceLocator!.BeginScope())
            {
                foreach(var component in _registeredComponents.Values)
                {
                    component.Resolve(_createdServiceLocator);
                }
            }
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                _createdServiceLocator?.Dispose();
            }
        }

        TComponent IServiceLocator.Resolve<TComponent>() where TComponent : class => _createdServiceLocator!.Resolve<TComponent>();
        TComponent[] IServiceLocator.ResolveAll<TComponent>() where TComponent : class => _createdServiceLocator!.ResolveAll<TComponent>();
        IDisposable IServiceLocator.BeginScope() => _createdServiceLocator!.BeginScope();
    }
}
