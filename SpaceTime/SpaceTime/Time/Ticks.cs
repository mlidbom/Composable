#region usings

using System;

#endregion

namespace Composable.SpaceTime.Time
{
    internal static class Ticks
    {
        public const long PerMicroSecond = TimeSpan.TicksPerMillisecond / 1000;
    }
}