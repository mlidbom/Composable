using System;
using System.Collections.Generic;
using AutoMapper;

namespace Composable.AutoMapper
{
    public class SafeConfiguration
    {
        private IDictionary<Type, ISet<Type>> _mapping = new Dictionary<Type, ISet<Type>>();
        private readonly Configuration _configuration;

        public SafeConfiguration(Configuration configuration)
        {
            this._configuration = configuration;
        }

        public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            InsertMappingHistoryOrThrowOnDuplicateMappings(typeof(TSource), typeof(TDestination));
            return _configuration.CreateMap<TSource,TDestination>();
        }

        public IMappingExpression CreateMap(Type sourceType, Type destinationType)
        {
            InsertMappingHistoryOrThrowOnDuplicateMappings(sourceType, destinationType);
            return _configuration.CreateMap(sourceType, destinationType);
        }

        private void InsertMappingHistoryOrThrowOnDuplicateMappings(Type sourceType, Type destinationType)
        {
            if (!_mapping.ContainsKey(sourceType))
            {
                _mapping[sourceType] = new HashSet<Type>();
            }
            
            if(_mapping[sourceType].Contains(destinationType))
            {
                throw new DuplicateMappingException(sourceType, destinationType);
            }

            _mapping[sourceType].Add(destinationType);
        }
    }
}