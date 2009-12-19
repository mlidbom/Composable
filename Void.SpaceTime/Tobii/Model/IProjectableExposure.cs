using System.Collections.Generic;
using Void.PlaneTime;

namespace Void.Tobii.Model
{
    public interface IProjectableExposure<T> : IExposure, IProjectablePlaneTimeInterval<T>
        where T : IProjectableExposure<T>
    {
    }

    public interface IExposure : IPlaneTimeInterval
    {
        IEnumerable<IMediaEvent> MediaEvents<T>() where T : IMediaEvent;
    }
}