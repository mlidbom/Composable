using System;
using System.Diagnostics.Contracts;

namespace Composable.System
{
    /// <summary>A collection of extensions to work with integers</summary>
    public static class IntegerExtensions
    {
        ///<summary>Executes <paramref name="action"/> <paramref name="times"/> times.</summary>
        public static void Times(this int times, Action action)
        {
            Contract.Requires(action != null);
            for(int i = 0; i < times; i++)
            {
                action();
            }
        }
    }
}