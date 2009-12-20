using System;

namespace Void.Plane
{
    /// <summary>A specific position in the plane.</summary>
    public interface IPlanePoint : IPlanePositioned
    {
        [Obsolete(WarningMessages.InteralOnly)] int XCoordinate { get; }
        [Obsolete(WarningMessages.InteralOnly)] int YCoordinate { get; }
    }

    public interface IPlaneMovement
    {
        [Obsolete(WarningMessages.InteralOnly)] int XMovement { get; }
        [Obsolete(WarningMessages.InteralOnly)] int YMovement { get; }
    }

    /// <summary>An object that resides at a <see cref="IPlanePoint"/>.</summary>
    public interface IPlanePositioned
    {
        /// The upper left corner of an imagined circumscribed rectangle(bounding box) around the object.
        IPlanePoint PlanePosition { get; }
    }

    /// <summary>An object capable of creating a clone of itself at another <see cref="IPlanePoint"/></summary>
    public interface IPlaneProjectable<T> : IPlanePositioned where T : IPlaneProjectable<T>
    {
        T ProjectAt(IPlanePoint targetPosition);
    }
}