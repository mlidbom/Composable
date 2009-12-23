using System;

namespace Void.Plane.Impl
{
    [Obsolete(WarningMessages.InternalAndInheritanceOnly)]
    internal class SimplePlanePositioned : IPlanePositioned
    {
        public IPlanePoint PlanePosition { get; private set; }

        /// <summary>Construct instance at specified <see cref="IPlanePoint"/></summary>
        protected SimplePlanePositioned(IPlanePoint position)
        {
            PlanePosition = position;
        }
    }
}