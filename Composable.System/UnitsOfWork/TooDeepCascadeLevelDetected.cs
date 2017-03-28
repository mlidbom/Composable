using System;
using Composable.System;

namespace Composable.UnitsOfWork
{
    class TooDeepCascadeLevelDetected : Exception
    {
        public TooDeepCascadeLevelDetected(int maxlevel)
            : base($"After calling commit on cascading unitoworkparticipants {maxlevel} times they still report that changes may occur")
        {

        }
    }
}