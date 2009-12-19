namespace Void.Plane.Impl
{
    public class SimplePlanePoint : IPlanePoint
    {
        public int XCoordinate { get; private set; }
        public int YCoordinate { get; private set; }

        public SimplePlanePoint(int xCoordinate, int yCoordinate)
        {
            XCoordinate = xCoordinate;
            YCoordinate = yCoordinate;
        }

        public SimplePlanePoint(IPlanePoint position) : this(position.XCoordinate, position.YCoordinate)
        {
        }
    }
}