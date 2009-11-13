using System;

namespace Void
{
    public static class EnumExtensions
    {
        public static bool HasFlag(this Enum value, Enum flag)
        {
            if (!value.GetType().Equals(flag.GetType()))
            {
                throw new ArgumentOutOfRangeException();
            }
            var longValue = Convert.ToInt64(value); ;
            var longFlag = Convert.ToInt64(flag);

            return (longValue & longFlag) == longFlag;
        }

    }
}