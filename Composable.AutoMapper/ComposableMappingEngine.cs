using System;
using System.Collections;
using System.Collections.Generic;
using AutoMapper;
using System.Linq;
using AutoMapper.Mappers;

namespace Composable.AutoMapper
{
    public static class ComposableMappingEngine
    {
        public static IMappingEngine BuildEngine(Action<SafeConfiguration> configureMappings)
        {
            return new MappingEngine(BuildConfiguration(configureMappings));
        }

        public static IConfigurationProvider BuildConfiguration(Action<SafeConfiguration> configureMappings)
        {
            var configuration = new ConfigurationStore(new TypeMapFactory(), MapperRegistry.Mappers);
            var config = new SafeConfiguration(configuration);

            configureMappings(config);
            configuration.AssertConfigurationIsValid();

            return configuration;
        }

        public static ComposableObjectMapper<TSource> Map<TSource>(this IMappingEngine engine, TSource source)
        {
            return new ComposableObjectMapper<TSource>(source, engine);
        }

        public static ComposableCollectionMapper<TSource> MapCollection<TSource>(this IMappingEngine engine, IEnumerable<TSource> source)
        {
            return new ComposableCollectionMapper<TSource>(source, engine);
        }

        public static TTarget MapTo<TTarget>(this object me, IMappingEngine engine)
        {
            return (TTarget)engine.Map(me, me.GetType(), typeof(TTarget));
        }

        public static IEnumerable<TTarget> MapCollectionTo<TTarget>(this IEnumerable me, IMappingEngine engine)
        {
            return me.Cast<object>().Select(obj => MapTo<TTarget>(obj, engine));
        }

        public static object MapTo(this object me, Type targetType, IMappingEngine engine)
        {
            return engine.Map(me, me.GetType(), targetType);
        }

        public static IEnumerable<object> MapCollectionTo(this IEnumerable me, Type targetType, IMappingEngine engine)
        {
            return me.Cast<object>().Select(obj => obj.MapTo(targetType, engine));
        }

        public static void MapOnto<TSource, TTarget>(this TSource source, TTarget target, IMappingEngine engine)
        {
            engine.Map(source, target);
        }

        public static void MapDynamicOnto<TSource, TTarget>(this TSource source, TTarget target, IMappingEngine engine)
        {
            engine.DynamicMap(source, target);
        }


        public class ComposableCollectionMapper<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly IMappingEngine _engine;

            public ComposableCollectionMapper(IEnumerable<TSource> source, IMappingEngine engine)
            {
                _source = source;
                _engine = engine;
            }

            public IEnumerable<TTarget> To<TTarget>()
            {
                return _source.Select(obj => _engine.Map<TSource, TTarget>(obj));
            }

            public IEnumerable<object> To(Type destinationType)
            {
                return _source.Select(obj => _engine.Map(obj, typeof(TSource), destinationType));
            }
        }

        public class ComposableObjectMapper<TSource>
        {
            private readonly TSource _source;
            private readonly IMappingEngine _engine;

            public ComposableObjectMapper(TSource source, IMappingEngine engine)
            {
                _source = source;
                _engine = engine;
            }

            public TTarget To<TTarget>()
            {
                return _engine.Map<TSource, TTarget>(_source);
            }

            public object To(Type destinationType)
            {
                return _engine.Map(_source, typeof(TSource), destinationType);
            }

            public TTarget OnTo<TTarget>(TTarget target)
            {
                return _engine.Map(_source, target);
            }

            public void DynamicOnto<TTarget>(TTarget target)
            {
                _engine.DynamicMap(_source, target);
            }
        }
    }    
}