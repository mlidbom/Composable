using System;

namespace Composable.Testing.System.Threading.ResourceAccess
{
    public class AwaitingUpdateNotificationTimedOutException : Exception
    {
        internal AwaitingUpdateNotificationTimedOutException() : base("Timed out awaiting an update notification.") { }
    }
}
