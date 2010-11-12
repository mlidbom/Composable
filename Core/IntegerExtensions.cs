using System;

namespace Void
{
    public static class IntegerExtensions
    {
        public static void Times(this int times, Action action)
        {
            for(int i = 0; i < times; i++)
            {
                action();
            }
        }
    }
}