using Void.Plane;

namespace Void.Shapes
{    
    /// The Width and Height are those of an imagined rectangle circumscribing the shape.</summary>
    public interface IShape
    {
        uint Width { get; }
        uint Height { get; }
        bool Contains(IPlanePoint point);
    }

    public interface IRectangle : IShape
    { }

    /// <summary>An <see cref="IPlanePositioned"/> with a width and height.</summary>
    public interface IPlanePositionedShape : IPlanePositioned, IShape
    { }

    public interface IPlaneProjectableShape<T> : IPlanePositionedShape, IPlaneProjectable<T> where T : IPlaneProjectableShape<T>
    { }    
}