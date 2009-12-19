using Void.PlaneTime;

namespace Void.Tobii.Model
{
    public interface IMedia
    {        
    }

    public interface IMediaEvent : IPlaneTimePositioned
    {
        IMedia Media { get; }    
    }

    public interface IProjectableMediaEvent<T> : IMediaEvent, IPlaneTimeProjectable<T>  where T : IProjectableMediaEvent<T>
    {        
    }
}