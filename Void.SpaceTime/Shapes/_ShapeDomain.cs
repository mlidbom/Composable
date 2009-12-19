using Void.Plane;

namespace Void.Shapes
{
    /// <summary>/// An <see cref="IPlanePositioned"/> with a positive width and height.
    /// The Width and Height are those of an imagined rectangle circumscribing the shape.</summary>
    public interface IShape
    {
        uint Width { get; }
        uint Height { get; }
        bool Contains(IPlanePoint point);
    }

    public interface IRectangle : IShape
    { }

    public interface IPositionedShape : IPlanePositioned, IShape
    { }

    public interface IPlaneProjectableShape<T> : IPositionedShape, IPlaneProjectable<T> where T : IPlaneProjectableShape<T>
    { }    
}