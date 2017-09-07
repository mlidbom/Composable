using System;
using System.Threading.Tasks;
using Composable.Testing.System.Configuration;
using Composable.Testing.System.Linq;

namespace Composable.Testing.System
{
    static class StrictlyManagedResources
    {
        public static readonly string CollectStackTracesForAllStrictlyManagedResourcesConfigurationParamaterName =
            ExpressionUtil.ExtractMemberPath(() => CollectStackTracesForAllStrictlyManagedResources);

        public static readonly bool CollectStackTracesForAllStrictlyManagedResources =
            AppConfigConfigurationParameterProvider.Instance.GetBoolean(CollectStackTracesForAllStrictlyManagedResourcesConfigurationParamaterName,
                                                                        valueIfMissing: false);

        public static bool CollectStackTracesFor<TManagedResource>()
            => AppConfigConfigurationParameterProvider.Instance.GetBoolean(ConfigurationParamaterNameFor<TManagedResource>(),
                                                                           valueIfMissing: false);

        static string ConfigurationParamaterNameFor<TManagedResource>() => ConfigurationParamaterNameFor(typeof(TManagedResource));

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
    interface IStrictlyManagedResource : IDisposable {}

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
    class StrictlyManagedResource<TManagedResource> : IStrictlyManagedResource where TManagedResource : IStrictlyManagedResource
    {
        // ReSharper disable once StaticMemberInGenericType
        public static Action<StrictlyManagedResourceWasFinalizedException> ThrowCreatedExceptionWhenFinalizerIsCalled = exception => throw exception;
        // ReSharper disable once StaticMemberInGenericType
        public static Action<StrictlyManagedResourceLifespanWasExceededException> ThrowCreatedExceptionWhenLifespanWasExceeded = exception => throw exception;

        static readonly bool CollectStacktraces = StrictlyManagedResources.CollectStackTracesFor<TManagedResource>();
        public StrictlyManagedResource(TimeSpan? maxLifetime = null, bool forceStackTraceCollection = false)
        {
            if(forceStackTraceCollection || CollectStacktraces || StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResources)
            {
                ReservationCallStack = Environment.StackTrace;
            }
            if(maxLifetime.HasValue)
            {
#pragma warning disable 4014
                ScheduleDisposalAndExistenceTest(this, maxLifetime.Value);
#pragma warning restore 4014
            }
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        static async Task ScheduleDisposalAndExistenceTest(StrictlyManagedResource<TManagedResource> resource, TimeSpan maxLifeSpan)
        {
            var resourceReference = new WeakReference<StrictlyManagedResource<TManagedResource>>(resource);
            await Task.Delay(maxLifeSpan).ConfigureAwait(false);
            StrictlyManagedResource<TManagedResource> stillLivingResource;
            if(resourceReference.TryGetTarget(out stillLivingResource) && !stillLivingResource._disposed)
            {
                ThrowCreatedExceptionWhenLifespanWasExceeded(new StrictlyManagedResourceLifespanWasExceededException(stillLivingResource.GetType(), stillLivingResource.ReservationCallStack, maxLifeSpan));
            }
        }

        string ReservationCallStack { get; }

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
                ThrowCreatedExceptionWhenFinalizerIsCalled(new StrictlyManagedResourceWasFinalizedException(GetType(), ReservationCallStack));
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
        protected StrictlyManagedResourceBase() => _strictlyManagedResource = new StrictlyManagedResource<TInheritor>();

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
    class StrictlyManagedResourceWasFinalizedException : Exception
    {
        public StrictlyManagedResourceWasFinalizedException(Type instanceType, string reservationCallStack) : base(FormatMessage(instanceType, reservationCallStack)) { }

        static string FormatMessage(Type instanceType, string reservationCallStack)
            => !reservationCallStack.IsNullOrWhiteSpace()
                   ? $@"User code failed to Dispose this instance of {instanceType.FullName}
Construction call stack: {reservationCallStack}"
                   : $@"No allocation stack trace collected. 
Set configuration value: {StrictlyManagedResources.ConfigurationParamaterNameFor(instanceType)} to ""true"" to collect allocation stack traces for this type.
Set configuration value: {StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResourcesConfigurationParamaterName} to ""true"" to collect allocation stack traces for all types.
Please note that this will decrease performance and should only be set while debugging resource leaks.";
    }

    class StrictlyManagedResourceLifespanWasExceededException : Exception
    {
        public StrictlyManagedResourceLifespanWasExceededException(Type instanceType, string reservationCallStack, TimeSpan maxTimeSpan) : base(FormatMessage(instanceType, reservationCallStack, maxTimeSpan)) { }

        static string FormatMessage(Type instanceType, string reservationCallStack, TimeSpan maxTimeSpan)
            => reservationCallStack != string.Empty
                   ? $@"User code failed to Dispose this instance of {instanceType.FullName} within the maximum lifetime: {maxTimeSpan}
Construction call stack: {reservationCallStack}"
                   : $@"No allocation stack trace collected. 
Set configuration value: {StrictlyManagedResources.ConfigurationParamaterNameFor(instanceType)} to ""true"" to collect allocation stack traces for this type.
Set configuration value: {StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResourcesConfigurationParamaterName} to ""true"" to collect allocation stack traces for all types.
Please note that this will decrease performance and should only be set while debugging resource leaks.";
    }
}


