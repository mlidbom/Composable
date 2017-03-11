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
    public sealed class TypedCollectionResolver<T> : ISubDependencyResolver
    {
        readonly bool _allowEmptyCollections;
        readonly IKernel _kernel;

        public TypedCollectionResolver(IKernel kernel, bool allowEmptyCollections = true)
        {
            _kernel = kernel;
            _allowEmptyCollections = allowEmptyCollections;
        }

        public bool CanResolve(CreationContext context,
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

        public object Resolve(CreationContext context,
                                      ISubDependencyResolver contextHandlerResolver,
                                      ComponentModel model,
                                      DependencyModel dependency)
        {
            return _kernel.ResolveAll(GetItemType(dependency.TargetItemType), null);
        }

        bool CanSatisfy(Type itemType)
        {
            return _allowEmptyCollections || _kernel.HasComponent(itemType);
        }

        static Type GetItemType(Type targetItemType)
        {
            return targetItemType.GetCompatibleArrayItemType();
        }

        static bool HasParameter(DependencyModel dependency)
        {
            return dependency.Parameter != null;
        }
    }
}