using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    public class AwaitingUpdateNotificationTimedOutException : Exception
    {
        internal AwaitingUpdateNotificationTimedOutException() : base("Timed out awaiting an update notification.") { }
    }
}
