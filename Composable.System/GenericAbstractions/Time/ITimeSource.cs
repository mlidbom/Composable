using System;

namespace Composable.GenericAbstractions.Time
{
    [Obsolete("Please switch to using IUtcTimeTimeSource")]
    ///<summary>Provides methods for determining the current time as local time or utc time.</summary>
    public interface ITimeSource : ILocalTimeTimeSource, IUtcTimeTimeSource
    {        
    }
}