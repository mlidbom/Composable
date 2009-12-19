namespace Void.Plane.Impl
{
    public class SimplePlanePoint : SimplePlanePositioned, IPlanePoint
    {
        public SimplePlanePoint(int xCoordinate, int yCoordinate) : base(xCoordinate, yCoordinate)
        {
        }

        protected SimplePlanePoint(IPlanePositioned position) : base(position)
        {
        }
    }
}