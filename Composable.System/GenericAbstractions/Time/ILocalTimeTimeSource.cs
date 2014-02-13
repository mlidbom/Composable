using System;

namespace Composable.GenericAbstractions.Time
{
    ///<summary>
    /// Provides the service of telling what the current time is using the timezone of the local computer. 
    /// In order to make things testable calling DateTime.Now or DateTime.UtcNow directly is discouraged.
    /// </summary>
    public interface ILocalTimeTimeSource
    {
        DateTime LocalNow { get; }
    }
}
