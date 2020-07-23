using System;

namespace Composable.SystemCE.Reflection.Threading.ResourceAccess
{
    public class AwaitingUpdateNotificationTimedOutException : Exception
    {
        internal AwaitingUpdateNotificationTimedOutException() : base("Timed out awaiting an update notification.") { }
    }
}
