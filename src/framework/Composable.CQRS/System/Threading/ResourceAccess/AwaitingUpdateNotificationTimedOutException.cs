using System;

namespace Composable.System.Threading.ResourceAccess
{
    public class AwaitingUpdateNotificationTimedOutException : Exception
    {
        internal AwaitingUpdateNotificationTimedOutException() : base("Timed out awaiting an update notification.") { }
    }
}
