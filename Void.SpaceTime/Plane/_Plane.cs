using Void.Shapes;

namespace Void.Plane
{
    /// <summary>A specific position in the plane.</summary>
    public interface IPlanePoint
    {
        int XCoordinate { get; }
        int YCoordinate { get; }
    }

    /// <summary>
    /// An object that resides at a <see cref="IPlanePoint"/>.    
    /// </summary>
    public interface IPlanePositioned
    {
        /// The upper left corner of an imagined circumscribed rectangle(bounding box).
        IPlanePoint PlanePosition { get; }
    }

    public interface IPlaneMovement
    {
        int XMovement { get; }
        int YMovement { get; }
    }

    public interface IPlaneProjectable<T> : IPlanePositioned where T : IPlaneProjectable<T>
    {
        T ProjectAt(IPlanePoint targetPosition);
    }
}