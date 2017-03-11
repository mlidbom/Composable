using System;
using Composable.System;

namespace Composable.DependencyInjection
{
    ///<summary>A lease to use a component.
    /// <para>Should be disposed as soon as the component is no longer in use.</para>
    /// <para>An exception is thrown if dispose fails to be called. </para>
    /// <para>should inherit from <see cref="StrictlyManagedResourceBase{TInheritor}"/> or have a member field of type: <see cref="StrictlyManagedResource{TManagedResource}"/></para>
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
        static IComponentLease<TComponent> Lease<TComponent>(this IServiceLocator @this) => (IComponentLease<TComponent>)@this.Lease(typeof(TComponent));

        // ReSharper disable once SuspiciousTypeConversion.Global
        static IComponentLease<TComponent[]> LeaseAll<TComponent>(this IServiceLocator @this) => (IComponentLease<TComponent[]>)@this.LeaseAll(typeof(TComponent));

        public static void Use<TComponent>(this IServiceLocator @this, Action<TComponent> useComponent)
        {
            using(var lease = @this.Lease<TComponent>())
            {
                useComponent(lease.Instance);
            }
        }

        public static TResult Use<TComponent, TResult>(this IServiceLocator @this, Func<TComponent, TResult> useComponent)
        {
            using(var lease = @this.Lease<TComponent>())
            {
                return useComponent(lease.Instance);
            }
        }

        public static void UseAll<TComponent>(this IServiceLocator @this, Action<TComponent[]> useComponent)
        {
            using(var lease = @this.LeaseAll<TComponent>())
            {
                useComponent(lease.Instance);
            }
        }

        public static TResult UseAll<TComponent, TResult>(this IServiceLocator @this, Func<TComponent[], TResult> useComponent)
        {
            using(var lease = @this.LeaseAll<TComponent>())
            {
                return useComponent(lease.Instance);
            }
        }
    }
}
