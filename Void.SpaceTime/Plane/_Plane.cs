namespace Void.Plane
{
    /// <summary>A specific position in the plane.</summary>
    public interface IPlanePoint
    {
        int XCoordinate { get; }
        int YCoordinate { get; }
    }

    /// <summary>An object that resides at a <see cref="IPlanePoint"/>.</summary>
    public interface IPlanePositioned
    {
        /// The upper left corner of an imagined circumscribed rectangle(bounding box) around the object.
        IPlanePoint PlanePosition { get; }
    }

    public interface IPlaneMovement
    {
        int XMovement { get; }
        int YMovement { get; }
    }

    /// <summary>An object capable of creating a clone of itself at another <see cref="IPlanePoint"/></summary>
    public interface IPlaneProjectable<T> : IPlanePositioned where T : IPlaneProjectable<T>
    {
        T ProjectAt(IPlanePoint targetPosition);
    }
}