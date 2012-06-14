#region usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.Mappers;
using Microsoft.Practices.ServiceLocation;

#endregion

namespace Composable.AutoMapper
{
    [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
    public static class ComposableMapper
    {      
        //todo:remove me
        private const string ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis = 
            "This member is being retired due to causing a bad case of staticitis. it will throw an ObsoletException if called! Please migrate to functionality in ComposableMappingEngine instead.";

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static void Init(Func<IMappingEngine> engineProvider)
        {
            throw new ObsoleteException();
        }

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static void Init(IServiceLocator locator)
        {
            throw new ObsoleteException();
        }

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static void ResetOnlyCallFromTests()
        {
            throw new ObsoleteException();
        }
        

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static TTarget MapTo<TTarget>(this object me)
        {
            throw new ObsoleteException();
        }

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static IEnumerable<TTarget> MapTo<TTarget>(this IEnumerable me)
        {
            throw new ObsoleteException();
        }

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static object MapTo(this object me, Type targetType)
        {
            throw new ObsoleteException();
        }

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static IEnumerable<object> MapTo(this IEnumerable me, Type targetType)
        {
            throw new ObsoleteException();
        }

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static void MapOnto<TSource, TTarget>(this TSource source, TTarget target)
        {
            throw new ObsoleteException();
        }

        [Obsolete(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis, false)]
        public static void MapDynamicOnto<TSource, TTarget>(this TSource source, TTarget target)
        {
            throw new ObsoleteException();
        }

        public class ObsoleteException : Exception
        {
            public ObsoleteException() : base(ThisMemberIsBeingRetiredDueToCausingABadCaseOfStaticitis)
            {}
        }
    }
}