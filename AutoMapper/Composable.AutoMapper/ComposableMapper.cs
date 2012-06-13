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
    [Obsolete(ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis, true)]
    public static class ComposableMapper
    {
        private const string ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis = "Composable.AutoMapper is being retired due to causing a bad case of staticitis";

        public static void Init(Func<IMappingEngine> engineProvider)
        {
            lock(LockObject)
            {
                if(_initialized)
                {
                    throw new Exception("You may only call Init once");
                }
                if(engineProvider == null)
                {
                    throw new ArgumentNullException("engineProvider");
                }
                _initialized = true;
                _engineProvider = engineProvider;
            }
        }

        public static void Init(IServiceLocator locator)
        {
            lock(LockObject)
            {
                if(_initialized)
                {
                    throw new Exception("You may only call Init once");
                }
                if(locator == null)
                {
                    throw new ArgumentNullException("locator");
                }
                _initialized = true;
                _locator = locator;
            }
        }

        public static void ResetOnlyCallFromTests()
        {
            lock(LockObject)
            {
                _initialized = false;
                _locator = null;
                _engineProvider = null;
            }
        }

        private static readonly object LockObject = new Object();
        private static Func<IMappingEngine> _engineProvider;
        private static IServiceLocator _locator;
        private static bool _initialized;

        private static IMappingEngine Engine
        {
            get
            {
                if(!_initialized)
                {
                    throw new InvalidOperationException("You must call init before using MapTo");
                }
                if(_engineProvider == null)
                {
                    lock(LockObject)
                    {
                        if(_engineProvider == null)
                        {
                            _engineProvider = CreateDefaultProvider();
                        }
                    }
                }
                return _engineProvider();
            }
        }

        private static Func<IMappingEngine> CreateDefaultProvider()
        {
            var configuration = new ConfigurationStore(new TypeMapFactory(), MapperRegistry.AllMappers());
            var safeConfiguration = new SafeConfiguration(configuration);

            foreach(var mappingCreator in _locator.GetAllInstances<IProvidesMappings>().ToArray())
            {
                mappingCreator.CreateMappings(safeConfiguration);
            }

            configuration.AssertConfigurationIsValid();

            var engine = new MappingEngine(configuration);

            return () => engine;
        }

        [Obsolete(ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis, true)]
        public static TTarget MapTo<TTarget>(this object me)
        {
            return (TTarget)Engine.Map(me, me.GetType(), typeof(TTarget));
        }

        [Obsolete(ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis, true)]
        public static IEnumerable<TTarget> MapTo<TTarget>(this IEnumerable me)
        {
            return me.Cast<object>().Select(MapTo<TTarget>);
        }

        [Obsolete(ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis, true)]
        public static object MapTo(this object me, Type targetType)
        {
            return Engine.Map(me, me.GetType(), targetType);
        }

        [Obsolete(ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis, true)]
        public static IEnumerable<object> MapTo(this IEnumerable me, Type targetType)
        {
            return me.Cast<object>().Select(obj => obj.MapTo(targetType));
        }

        [Obsolete(ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis, true)]
        public static void MapOnto<TSource, TTarget>(this TSource source, TTarget target)
        {
            Engine.Map(source, target);
        }

        [Obsolete(ComposableAutomapperIsBeingRetiredDueToCausingABadCaseOfStaticitis, true)]
        public static void MapDynamicOnto<TSource, TTarget>(this TSource source, TTarget target)
        {
            Engine.DynamicMap(source, target);
        }
    }
}