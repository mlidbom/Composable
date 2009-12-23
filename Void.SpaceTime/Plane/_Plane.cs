using System;

namespace Void.Plane
{
    /// <summary>A specific point in the plane. A point has zero width and height.</summary>
    public interface IPlanePoint : IPlanePositioned
    {
        /// <summary>The distance to the right from the upper left corner of the plane to this point..</summary>
        [Obsolete(WarningMessages.InteralOnly)] int XCoordinate { get; }
        /// <summary>The distance downward from the upper left corner of the plane to this point..</summary>
        [Obsolete(WarningMessages.InteralOnly)] int YCoordinate { get; }
    }

    /// <summary>Represents a vector (arrow) in the plane</summary>
    public interface IPlaneMovement
    {
        /// <summary>The left-right distance of this movement</summary>
        [Obsolete(WarningMessages.InteralOnly)] int XMovement { get; }

        /// <summary>The up-down distance of this movement</summary>
        [Obsolete(WarningMessages.InteralOnly)] int YMovement { get; }
    }

    /// <summary>An object that resides at a <see cref="IPlanePoint"/>.</summary>
    public interface IPlanePositioned
    {
        /// The upper left corner of an imagined circumscribed rectangle(bounding box) around the object.
        IPlanePoint PlanePosition { get; }
    }

    /// <summary>An object capable of creating a clone of itself at another <see cref="IPlanePoint"/></summary>
    /// <typeparam name="TProjection">The type of the projected clone.</typeparam>
    public interface IPlaneProjectable<TProjection> : IPlanePositioned where TProjection : IPlaneProjectable<TProjection>
    {
        /// <summary>Create a clone of the object positioned at <paramref name="targetPosition"/></summary>
        TProjection ProjectAt(IPlanePoint targetPosition);
    }
}