using Void.Plane;
using Void.PlaneTime;
using Void.PlaneTime.Impl;
using Void.Time;
using Void.Tobii.Model;

namespace Void.Tobii.Impl
{
    public class ApiExposure : SimplePlaneTimeInterval, IProjectableExposure<ApiExposure>
    {
        public ApiExposure(IPlanePoint position, ITimeInterval time) : base(position, time)
        {
        }

        public ApiExposure(IPlaneTimeInterval position) : base(position, position)
        {
        }

        public ApiExposure ProjectAt(ITimePoint targetTime)
        {
            return new ApiExposure(PlaneTimeInterval.MoveTo(this, targetTime));
        }

        public ApiExposure ProjectAt(IPlanePoint targetPosition)
        {
            return new ApiExposure(PlaneTimeInterval.MoveTo(this, targetPosition));
        }
    }
}