using System;

namespace Void.Plane.Impl
{
    public class SimplePlanePositioned : IPlanePositioned
    {
        public IPlanePoint PlanePosition { get; private set; }
        protected SimplePlanePositioned(IPlanePoint position)
        {
            PlanePosition = position;
        }        
    }
}