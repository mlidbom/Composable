using System;
using System.Diagnostics;
using Composable.System.Configuration;
using Composable.System.Linq;

namespace Composable.System
{
    static class StrictlyManagedResources
    {
        public static readonly string CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName =
            ExpressionUtil.ExtractMemberPath(() => CollectStackTracesForAllStrictlyManagedResources);

        public static readonly bool CollectStackTracesForAllStrictlyManagedResources =
            AppConfigConfigurationParameterProvider.Instance.GetBoolean(CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName,
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
        static readonly bool CollectStackTraces = StrictlyManagedResources.CollectStackTracesFor<TManagedResource>();
        public StrictlyManagedResource(bool forceStackTraceCollection = false, bool needsFileInfo = false)
        {
            if(forceStackTraceCollection || CollectStackTraces || StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResources)
            {
                ReservationCallStack = new StackTrace(fNeedFileInfo:needsFileInfo).ToString();
            }
        }

        string? ReservationCallStack { get; }

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
                throw new StrictlyManagedResourceWasFinalizedException(GetType(), ReservationCallStack);
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
        protected StrictlyManagedResourceBase(bool forceStackTraceAllocation = false, bool needsFileInfo = false) => _strictlyManagedResource = new StrictlyManagedResource<TInheritor>(forceStackTraceAllocation, needsFileInfo);

        public void Dispose()
        {
            //todo: _disposed should be set to true before calling something that might conceivable cause reentrancy...
            GC.SuppressFinalize(this);
            if(!_disposed)
            {
                _disposed = true;
                _strictlyManagedResource.Dispose();
                InternalDispose();
            }
        }

        protected abstract void InternalDispose();
    }

    ///<summary><see cref="IStrictlyManagedResource"/></summary>
    class StrictlyManagedResourceWasFinalizedException : Exception
    {
        public StrictlyManagedResourceWasFinalizedException(Type instanceType, string? reservationCallStack) : base(FormatMessage(instanceType, reservationCallStack)) { }

        static string FormatMessage(Type instanceType, string? reservationCallStack)
            => !reservationCallStack.IsNullEmptyOrWhiteSpace()
                   ? $@"User code failed to Dispose this instance of {instanceType.FullName}
Construction call stack: {reservationCallStack}"
                   : $@"No allocation stack trace collected. 
Set configuration value: {StrictlyManagedResources.ConfigurationParamaterNameFor(instanceType)} to ""true"" to collect allocation stack traces for this type.
Set configuration value: {StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName} to ""true"" to collect allocation stack traces for all types.
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
Set configuration value: {StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName} to ""true"" to collect allocation stack traces for all types.
Please note that this will decrease performance and should only be set while debugging resource leaks.";
    }
}


