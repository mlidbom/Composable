using System;
using System.Threading.Tasks;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    static class NetMqPollerExtensions
    {
        internal static void RunOnPollerThread(this NetMQPoller @this, Action action) => @this.RunOnPollerThread(new Task(action));
        internal static void RunOnPollerThread(this NetMQPoller @this, Task task) => task.Start(@this);
    }
}
