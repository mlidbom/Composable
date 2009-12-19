using Void.Plane;
using Void.Time;

namespace Void.PlaneTime
{
    public interface IPlaneTimePoint : ITimePoint, IPlanePoint
    {
    }

    public interface IPlaneTimePositioned : ITimePositioned, IPlanePositioned
    {
    }

    public interface IPlaneTimeInterval : IPlaneTimePositioned, ITimeInterval
    {
    }

    public interface IPlaneTimeProjectable<T> : IPlaneTimePositioned, IPlaneProjectable<T>, ITimeProjectable<T>
        where T : ITimeProjectable<T>, IPlaneProjectable<T>
    {
    }

    public interface IProjectablePlaneTimeInterval<T> : IPlaneTimeInterval, IPlaneTimeProjectable<T>
        where T : IProjectablePlaneTimeInterval<T>
    {
    }
}