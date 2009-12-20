using System.Collections.Generic;
using Void.PlaneTime;

namespace Void.Tobii.Model
{
    public interface IMedia
    {
    }

    public interface IMediaEvent : IPlaneTimeInterval
    {
        IMedia Media { get; }
    }

    public interface IProjectableMediaEvent<T> : IMediaEvent, IProjectablePlaneTimeInterval<T>
        where T : IProjectableMediaEvent<T>
    {
    }

    public interface IExposure : IPlaneTimeInterval
    {
        IEnumerable<IMediaEvent> MediaEvents<T>() where T : IMediaEvent;
    }

    public interface IProjectableExposure<T> : IExposure, IProjectablePlaneTimeInterval<T>
        where T : IProjectableExposure<T>
    {
    }
}