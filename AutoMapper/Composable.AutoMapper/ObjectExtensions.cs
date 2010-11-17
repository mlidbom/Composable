using System.Collections;
using System.Collections.Generic;
using AutoMapper;
using System.Linq;

namespace Composable.AutoMapper
{
    public static class MapperExtensions
    {
        public static TTarget MapTo<TTarget>(this object me)
        {
            return (TTarget) Mapper.Map(me, me.GetType(), typeof (TTarget));
        }

        public static IEnumerable<TTarget> MapTo<TTarget>(this IEnumerable me)
        {
            return me.Cast<object>().Select(MapTo<TTarget>);
        }
    }
}