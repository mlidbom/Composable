#region usings

using Composable.SpaceTime.Plane;
using Composable.SpaceTime.Time;

#endregion

namespace Composable.SpaceTime.PlaneTime
{
    /// <summary>A point in the plane and in time</summary>
    public interface IPlaneTimePoint : ITimePoint, IPlanePoint
    {
    }

    /// <summary>Something that has a position in the plane and in time.</summary>
    public interface IPlaneTimePositioned : ITimePositioned, IPlanePositioned
    {
    }

    /// <summary>Something that occupies at a point in the plane and an interval in time.</summary>
    public interface IPlaneTimeInterval : IPlaneTimePositioned, ITimeInterval
    {
    }

    /// <summary>Something that can project a clone of itself to other positions in the plane and in time.</summary>
    /// <typeparam name="TProjection">The type of the projected clone.</typeparam>
    public interface IPlaneTimeProjectable<TProjection> : IPlaneTimePositioned, IPlaneProjectable<TProjection>,
                                                          ITimeProjectable<TProjection>
        where TProjection : ITimeProjectable<TProjection>, IPlaneProjectable<TProjection>
    {
    }

    /// <summary>An <see cref="IPlaneTimeInterval"/> that can can project a clone of itself to other positions in the plane and in time.</summary>
    /// <typeparam name="TProjection">The type of the projected clone.</typeparam>
    public interface IProjectablePlaneTimeInterval<TProjection> : IPlaneTimeInterval, IPlaneTimeProjectable<TProjection>
        where TProjection : IProjectablePlaneTimeInterval<TProjection>
    {
    }
}