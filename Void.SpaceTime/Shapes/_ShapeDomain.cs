using Composable.SpaceTime.Plane;

namespace Composable.SpaceTime.Shapes
{    
    /// <summary>Something that occupies an area in the plane.</summary>
    public interface IShape
    {
        /// <summary>The width of a circumscribed rectangle.</summary>
        uint Width { get; }
        /// <summary>The height of a circumscribed rectangle.</summary>
        uint Height { get; }
    }

    /// <summary>A rectangular <see cref="IShape"/></summary>
    public interface IRectangle : IShape
    { }

    /// <summary>An <see cref="IPlanePositioned"/> <see cref="IRectangle"/>.</summary>
    public interface IPlanePositionedShape : IPlanePositioned, IShape
    { }

    /// <summary>An <see cref="IPlaneProjectable{TProjection}"/> <see cref="IShape"/> </summary>
    public interface IPlaneProjectableShape<T> : IPlanePositionedShape, IPlaneProjectable<T> where T : IPlaneProjectableShape<T>
    { }    
}