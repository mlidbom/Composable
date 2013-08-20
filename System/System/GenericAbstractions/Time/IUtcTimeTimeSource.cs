using System;

namespace Composable.GenericAbstractions.Time
{
    ///<summary>
    /// Provides the service of telling what the current UTC time is. 
    /// In order to make things testable calling DateTime.Now or DateTime.UtcNow directly is discouraged.
    /// </summary>
    public interface IUtcTimeTimeSource
    {
        DateTime UtcNow { get; }
    }
}