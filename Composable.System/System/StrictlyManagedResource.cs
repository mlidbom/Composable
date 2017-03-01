namespace Composable.System
{
  using global::System;

  public class StrictlyManagedResource : IDisposable
  {
    public StrictlyManagedResource() { ReservationCallStack = Environment.StackTrace; }

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
        throw new ResourceWasLeakedException(GetType(), ReservationCallStack);
      }
    }
  }

  public abstract class StrictlyManagedResourceBase : IDisposable
  {
    bool _disposed;
    readonly StrictlyManagedResource _strictlyManagedResource;
    protected StrictlyManagedResourceBase()
    {
      _strictlyManagedResource = new StrictlyManagedResource();
    }    

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

  public class ResourceWasLeakedException : Exception
  {
    public ResourceWasLeakedException(Type instanceType, string reservationCallStack)
      : base(FormatMessage(instanceType, reservationCallStack)) { }

    static string FormatMessage(Type instanceType, string reservationCallStack) { return $@"User code failed to InternalDispose this instance of {instanceType.FullName}
Construction call stack: {reservationCallStack}"; }
  }
}
