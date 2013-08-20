using System;

namespace Composable.GenericAbstractions.Time
{
    ///<summary>Simply returns DateTime.Now or DateTime.UtcNow</summary>
    public class DateTimeNowTimeSource : ITimeSource
    {
        ///<summary>Returns DateTime.Now</summary>
        public DateTime LocalNow { get { return DateTime.Now; } }

        ///<summary>Returns DateTime.UtcNow</summary>
        public DateTime UtcNow { get { return DateTime.UtcNow; } }
    }
}
