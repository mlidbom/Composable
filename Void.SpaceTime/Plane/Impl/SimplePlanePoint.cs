namespace Void.Plane.Impl
{
    public class SimplePlanePoint : IPlanePoint
    {
        public IPlanePoint PlanePosition { get{ return this;} }
        public int XCoordinate { get; private set; }
        public int YCoordinate { get; private set; }

        public SimplePlanePoint(int xCoordinate, int yCoordinate)
        {
            XCoordinate = xCoordinate;
            YCoordinate = YCoordinate;
        }

        public SimplePlanePoint(IPlanePoint position):this(position.XCoordinate, position.YCoordinate)
        {
        }
    }
}