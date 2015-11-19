using System;
using Castle.Core;
using Castle.Core.Internal;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace Composable.CQRS.Windsor
{
    /// <summary>
    /// A collection resolver based of the default CollectionResolver in Castle Windsor but it only resolves collections of the specified type (T)
    /// Use it by adding it to the container at wire-up with container.Kernel.Resolver.AddSubResolver(new TypedCollectionResolver&lt;CollectionItemType&gt;(container.Kernel));
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public class TypedCollectionResolver<T> : ISubDependencyResolver
    {
        private readonly bool _allowEmptyCollections;
        private readonly IKernel _kernel;

        public TypedCollectionResolver(IKernel kernel, bool allowEmptyCollections = true)
        {
            _kernel = kernel;
            _allowEmptyCollections = allowEmptyCollections;
        }

        public virtual bool CanResolve(CreationContext context,
                                       ISubDependencyResolver contextHandlerResolver,
                                       ComponentModel model,
                                       DependencyModel dependency)
        {
            if (dependency.TargetItemType == null)
            {
                return false;
            }

            var itemType = GetItemType(dependency.TargetItemType);

            return itemType != null &&
                   itemType == typeof(T) &&
                   HasParameter(dependency) == false &&
                   CanSatisfy(itemType);
        }

        public virtual object Resolve(CreationContext context,
                                      ISubDependencyResolver contextHandlerResolver,
                                      ComponentModel model,
                                      DependencyModel dependency)
        {
            return _kernel.ResolveAll(GetItemType(dependency.TargetItemType), null);
        }

        protected virtual bool CanSatisfy(Type itemType)
        {
            return _allowEmptyCollections || _kernel.HasComponent(itemType);
        }

        protected virtual Type GetItemType(Type targetItemType)
        {
            return targetItemType.GetCompatibleArrayItemType();
        }

        protected virtual bool HasParameter(DependencyModel dependency)
        {
            return dependency.Parameter != null;
        }
    }
}