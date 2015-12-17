using System;

namespace Composable.GenericAbstractions.Time
{
    ///<summary>Simply returns DateTime.Now or DateTime.UtcNow</summary>
    public class DateTimeNowTimeSource : IUtcTimeTimeSource
    {
        public static readonly DateTimeNowTimeSource Instance = new DateTimeNowTimeSource();      

        ///<summary>Returns DateTime.UtcNow</summary>
        public DateTime UtcNow { get { return DateTime.UtcNow; } }
    }
}
