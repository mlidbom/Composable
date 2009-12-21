using System;

namespace Void.Plane.Impl
{
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
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

        public IPlanePoint PlanePosition { get { return this; } }
    }
}