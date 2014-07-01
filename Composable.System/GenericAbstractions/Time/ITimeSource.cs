namespace Composable.GenericAbstractions.Time
{
    ///<summary>Provides methods for determining the current time as local time or utc time.</summary>
    public interface ITimeSource : ILocalTimeTimeSource, IUtcTimeTimeSource
    {        
    }
}