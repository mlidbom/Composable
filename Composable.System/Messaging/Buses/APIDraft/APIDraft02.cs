using System.Collections.Generic;
// ReSharper disable All

namespace Composable.Messaging.Buses.APIDraft
{
    public class APIDraft02
    {
        interface IThreadingPolicy
        {
            IEnumerable<string> LocksToTake();
        }

        class ThreadingPolicy
        {
            
        }
    }
}
