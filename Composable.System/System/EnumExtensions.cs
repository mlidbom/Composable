#region usings

using System;
using System.Diagnostics.Contracts;

#endregion

namespace Composable.System
{
    /// <summary/>
    [Pure]
    public static class EnumExtensions
    {
        /// <summary>
        /// True if <paramref name="value"/> contains the bit flag <paramref name="flag"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global todo: tests
        public static bool HasFlag(this Enum value, Enum flag)
        {
            Contract.Requires(value != null && flag != null);

            if(!value.GetType().Equals(flag.GetType()))
            {
                throw new ArgumentOutOfRangeException();
            }
            var longValue = Convert.ToInt64(value);
            var longFlag = Convert.ToInt64(flag);

            return (longValue & longFlag) == longFlag;
        }
    }
}