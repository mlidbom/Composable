using System;
using System.Diagnostics;
using Composable.Logging;
using Composable.SystemCE.ConfigurationCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReflectionCE;

namespace Composable.SystemCE
{
    static class StrictlyManagedResources
    {
        public static readonly string CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName =
            ExpressionUtil.ExtractMemberPath(() => CollectStackTracesForAllStrictlyManagedResources);

        public static readonly bool CollectStackTracesForAllStrictlyManagedResources =
            AppSettingsJsonConfigurationParameterProvider.Instance.GetBoolean(CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName,
                                                                              valueIfMissing: false);

        public static bool CollectStackTracesFor<TManagedResource>()
            => AppSettingsJsonConfigurationParameterProvider.Instance.GetBoolean(ConfigurationParamaterNameFor<TManagedResource>(),
                                                                                 valueIfMissing: false);

        static string ConfigurationParamaterNameFor<TManagedResource>() => ConfigurationParamaterNameFor(typeof(TManagedResource));

        public static string ConfigurationParamaterNameFor(Type instanceType) => $"{instanceType.FullName}.CollectStackTraces";
    }

    ///<summary>
    /// A strictly managed resource logs an Exception of type <see cref="StrictlyManagedResourceWasFinalizedException"/> if the finalizer is ever called.
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
        static readonly object StaticLock = new object();
        static bool _collectStackTraces = StrictlyManagedResources.CollectStackTracesFor<TManagedResource>();
        public StrictlyManagedResource(bool forceStackTraceCollection = false, bool needsFileInfo = false)
        {
            if(forceStackTraceCollection || _collectStackTraces || StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResources)
            {
                ReservationCallStack = new StackTrace(fNeedFileInfo: needsFileInfo).ToString();
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
                //Don't even think about letting exceptions escape on the finalizer thread again.The day I spent trying to understand why test processes simply died without explanation was no fun. Once was plenty.
                try
                {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                    throw new StrictlyManagedResourceWasFinalizedException(GetType(), ReservationCallStack);
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
                }
                catch(StrictlyManagedResourceWasFinalizedException exception)
                {
                    try
                    {
                        this.Log().Error(exception);
                        //Todo: Log metric here.
                        lock(StaticLock)
                        {
                            if(!_collectStackTraces)
                            {
                                this.Log().Warning($"Enabling collection of stacktraces for {typeof(TManagedResource).GetFullNameCompilable()} since it is not always disposed.");
                                _collectStackTraces = true;
                            }
                        }
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch {}
                }
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                _disposed = true;
                _strictlyManagedResource.Dispose();
            }
        }
    }

    ///<summary><see cref="IStrictlyManagedResource"/></summary>
    public class StrictlyManagedResourceWasFinalizedException : Exception
    {
        public StrictlyManagedResourceWasFinalizedException(Type instanceType, string? reservationCallStack) : base(FormatMessage(instanceType, reservationCallStack)) {}

        static string FormatMessage(Type instanceType, string? reservationCallStack)
            => !reservationCallStack.IsNullEmptyOrWhiteSpace()
                   ? $@"User code failed to Dispose this instance of {instanceType.GetFullNameCompilable()}
Construction call stack: {reservationCallStack}"
                   : $@"No allocation stack trace collected. 
Set configuration value: {StrictlyManagedResources.ConfigurationParamaterNameFor(instanceType)} to ""true"" to collect allocation stack traces for this type.
Set configuration value: {StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName} to ""true"" to collect allocation stack traces for all types.
Please note that this will decrease performance and should only be set while debugging resource leaks.";
    }

    public class StrictlyManagedResourceLifespanWasExceededException : Exception
    {
        public StrictlyManagedResourceLifespanWasExceededException(Type instanceType, string reservationCallStack, TimeSpan maxTimeSpan) : base(FormatMessage(instanceType, reservationCallStack, maxTimeSpan)) {}

        static string FormatMessage(Type instanceType, string reservationCallStack, TimeSpan maxTimeSpan)
            => !reservationCallStack.IsNullEmptyOrWhiteSpace()
                   ? $@"User code failed to Dispose this instance of {instanceType.FullName} within the maximum lifetime: {maxTimeSpan}
Construction call stack: {reservationCallStack}"
                   : $@"No allocation stack trace collected. 
Set configuration value: {StrictlyManagedResources.ConfigurationParamaterNameFor(instanceType)} to ""true"" to collect allocation stack traces for this type.
Set configuration value: {StrictlyManagedResources.CollectStackTracesForAllStrictlyManagedResourcesConfigurationParameterName} to ""true"" to collect allocation stack traces for all types.
Please note that this will decrease performance and should only be set while debugging resource leaks.";
    }
}
