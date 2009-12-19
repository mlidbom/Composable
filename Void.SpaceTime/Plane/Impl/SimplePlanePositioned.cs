namespace Void.Plane.Impl
{
    public class SimplePlanePositioned : IPlanePositioned
    {
        public int XCoordinate { get; private set; }
        public int YCoordinate { get; private set; }

        protected SimplePlanePositioned(IPlanePositioned position)
            : this(position.XCoordinate, position.YCoordinate)
        {
        }

        public SimplePlanePositioned(int xCoordinate, int yCoordinate)
        {
            XCoordinate = xCoordinate;
            YCoordinate = yCoordinate;
        }
    }
}