using System;
using System.Drawing;
using Composable.SpaceTime.Plane.Impl;

namespace Composable.SpaceTime.Plane
{
    /// <summary>Provides extensions to <see cref="IPlanePoint"/></summary>
    public static class PlanePoint
    {
        /// <summary></summary>
        public static IPlanePoint AsPlanePoint(this Point point)
        {
            return SimplePlanePoint.FromXAndY(point.X, point.Y);
        }
    }
}