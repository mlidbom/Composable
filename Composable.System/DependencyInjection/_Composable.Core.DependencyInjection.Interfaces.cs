using System;
using Composable.System;

namespace Composable.DependencyInjection
{
    ///<summary>A lease to use a component.
    /// <para>Should be disposed as soon as the component is no longer in use.</para>
    /// <para>An exception is thrown if dispose fails to be called. </para>
    /// <para>should inherit from <see cref="StrictlyManagedResourceBase"/> or have a member field of type: <see cref="StrictlyManagedResource"/></para>
    /// </summary>
    public interface IComponentLease<out TComponent> : IDisposable
    {
        TComponent Instance { get; }
    }

    ///<summary></summary>
    public interface IServiceLocator
    {
        IComponentLease<object> Lease(Type componentType);
        IComponentLease<object[]> LeaseAll(Type componentType);
    }

    public static class ServiceLocator
    {
        public static IComponentLease<TComponent> Lease<TComponent>(this IServiceLocator @this) => (IComponentLease<TComponent>)@this.Lease(typeof(TComponent));
        // ReSharper disable once SuspiciousTypeConversion.Global
        public static IComponentLease<TComponent[]> LeaseAll<TComponent>(this IServiceLocator @this) => (IComponentLease<TComponent[]>)@this.LeaseAll(typeof(TComponent));
    }
}
