using System;

namespace Composable.GenericAbstractions.Time
{
    ///<summary>Simply returns DateTime.Now or DateTime.UtcNow</summary>
    public class DateTimeNowTimeSource : IUtcTimeTimeSource
    {
        ///<summary>Returns an instance.</summary>
        internal static readonly DateTimeNowTimeSource Instance = new DateTimeNowTimeSource();

        ///<summary>Returns DateTime.UtcNow</summary>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
