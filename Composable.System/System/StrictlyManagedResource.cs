using System;
using Composable.System.Configuration;
using Composable.System.Linq;

namespace Composable.System
{
    static class StrictlyManagedResources
    {
        public static readonly string CollectStackTracesForAllStrictlyManagedResourcesConfigurationParamaterName =
            ExpressionUtil.ExtractMemberPath(() => CollectStackTracesForAllStrictlyManagedResources);

        public static readonly bool CollectStackTracesForAllStrictlyManagedResources =
            AppConfigConfigurationParameterProvider.Instance.GetBoolean(CollectStackTracesForAllStrictlyManagedResourcesConfigurationParamaterName,
                                                                        valueIfMissing: false);

        public static bool CollectStackTracesFor<TManagedResource>()
            => AppConfigConfigurationParameterProvider.Instance.GetBoolean(StrictlyManagedResources.ConfigurationParamaterNameFor<TManagedResource>(),
                                                                           valueIfMissing: false);

        public static string ConfigurationParamaterNameFor<TManagedResource>() => ConfigurationParamaterNameFor(typeof(TManagedResource));

        public static string ConfigurationParamaterNameFor(Type instanceType) => $"{instanceType.FullName}.CollectStackTraces";
    }

    ///<summary>
    /// A strictly managed resource guarantees that an Exception of type <see cref="StrictlyManagedResourceWasFinalizedException"/> is thrown if the finalizer is ever called.
    /// <para>In other word: If a user of an instance fails to correctly call <see cref="IDisposable.Dispose"/> on a <see cref="StrictlyManagedResourceWasFinalizedException"/>
    /// will be thrown when the instance is eventually finalized by the garbage collector.</para>
    /// This helps to guarantee that your application has no resource leaks.
    /// <para>Implementing this interface MUST be done by inheriting from <see cref="StrictlyManagedResourceBase{TInheritor}"/> or having a readonly field of type <see cref="StrictlyManagedResource{TManagedResource}"/>.
    ///  This guarantees the expected behavior including the ability to enable and disable the collection of stacktraces for the allocations.</para>
    /// </summary>
    public interface IStrictlyManagedResource : IDisposable {}

    ///<summary>
    /// Helper class for implementing <see cref="IStrictlyManagedResource"/>
    /// </summary>
    /// <example>
    /// Typical usage is to implement <see cref="IStrictlyManagedResource"/> by having a <see cref="StrictlyManagedResource{TManagedResource}"/> instance field:
    /// <code>
    ///class AnotherStrictlyManagedResource : SomeBaseClass, IStrictlyManagedResource
    ///{
    ///    readonly StrictlyManagedResource _leakDetector =  new StrictlyManagedResource();
    ///    public void Dispose()
    ///    {
    ///        GC.SuppressFinalize(this);
    ///        _leakDetector.Dispose();
    ///    }
    ///}
    /// </code>
    ///</example>
    public class StrictlyManagedResource<TManagedResource> : IStrictlyManagedResource where TManagedResource : IStrictlyManagedResource
    {
        public static Action<StrictlyManagedResourceWasFinalizedException> ThrowCreatedException = exception => { throw exception; };
        static readonly bool CollectStacktraces = StrictlyManagedResources.CollectStackTracesFor<TManagedResource>();
        public StrictlyManagedResource(TimeSpan? maxLifetime = null, bool forceStackTraceCollection = false)
        {
            if(forceStackTraceCollection || CollectStacktraces || StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResources)
            {
                ReservationCallStack = Environment.StackTrace;
            }
        }

        public string ReservationCallStack { get; }

        bool _disposed;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        ~StrictlyManagedResource()
        {
            if(!_disposed)
            {
                ThrowCreatedException(new StrictlyManagedResourceWasFinalizedException(GetType(), ReservationCallStack));
            }
        }
    }

    ///<summary>
    /// Inheriting from this class is the simplest way to implement <see cref="IStrictlyManagedResource"/>
    ///</summary>
    ///<example>
    ///<code>
    ///class SomeStrictlyManagedResource : StrictlyManagedResourceBase
    ///{
    ///    ResourceThatMustBeDisposed _resourceThatMustBeDisposed = new ResourceThatMustBeDisposed();
    ///    bool _disposed;
    ///    protected override void InternalDispose()
    ///    {
    ///        if (!_disposed)
    ///        {
    ///           _disposed = true;
    ///           _resourceThatMustBeDisposed.Dispose();
    ///        }
    ///    }
    ///}
    ///</code>
    ///</example>
    public abstract class StrictlyManagedResourceBase<TInheritor> : IStrictlyManagedResource where TInheritor : StrictlyManagedResourceBase<TInheritor>
    {
        bool _disposed;
        readonly StrictlyManagedResource<TInheritor> _strictlyManagedResource;
        protected StrictlyManagedResourceBase() { _strictlyManagedResource = new StrictlyManagedResource<TInheritor>(); }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if(!_disposed)
            {
                _strictlyManagedResource.Dispose();
                InternalDispose();
            }

            _disposed = true;
        }

        protected abstract void InternalDispose();
    }

    ///<summary><see cref="IStrictlyManagedResource"/></summary>
    public class StrictlyManagedResourceWasFinalizedException : Exception
    {
        public StrictlyManagedResourceWasFinalizedException(Type instanceType, string reservationCallStack) : base(FormatMessage(instanceType, reservationCallStack)) { }

        static string FormatMessage(Type instanceType, string reservationCallStack)
            => reservationCallStack != string.Empty
                   ? $@"User code failed to Dispose this instance of {instanceType.FullName}
Construction call stack: {reservationCallStack}"
                   : $@"No allocation stack trace collected. 
Set configuration value: {StrictlyManagedResources.ConfigurationParamaterNameFor(instanceType)} to ""true"" to collect allocation stack traces for this type.
Set configuration value: {StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResourcesConfigurationParamaterName} to ""true"" to collect allocation stack traces for all types.
Please note that this will decrease performance and should only be set while debugging resource leaks.";
    }
}
