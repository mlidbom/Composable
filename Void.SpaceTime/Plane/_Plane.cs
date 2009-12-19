namespace Void.Plane
{
    /// <summary>
    /// An object with a position in the plane.
    /// 
    /// The coordinates are the upper lefct corner of an imagined rectangle circumscribing the shape.
    /// </summary>
    public interface IPlanePositioned
    {
        int XCoordinate { get; }
        int YCoordinate { get; }
    }

    /// <summary>An <see cref="IPlanePositioned"/> with zero width and height.</summary>
    public interface IPlanePoint : IPlanePositioned
    {
    }

    /// <summary>/// An <see cref="IPlanePositioned"/> with a positive width and height.
    /// The Width and Height are those of an imagined rectangle circumscribing the shape.</summary>
    public interface IShape
    {
        uint Width { get; }
        uint Height { get; }
        bool Contains(IPlanePoint point);
    }

    public interface IRectangle : IShape
    {}

    public interface IPositionedShape : IPlanePositioned, IShape
    {}

    public interface IPlaneMovement
    {
        int XMovement { get; }
        int YMovement { get; }
    }

    public interface IPlaneProjectable<T> : IPlanePoint where T : IPlaneProjectable<T>
    {
        T ProjectAt(IPlanePoint targetPosition);
    }

    public interface IPlaneProjectableShape<T> : IPositionedShape, IPlaneProjectable<T> where T : IPlaneProjectable<T>
    {}    
}