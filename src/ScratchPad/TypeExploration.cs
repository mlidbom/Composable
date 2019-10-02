﻿using System;
using System.Collections.Generic;
using System.Linq;
using AccountManagement.Domain;
using Composable;
using Composable.Messaging;
using Composable.Refactoring.Naming;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.Testing.Performance;
using NCrunch.Framework;
using NUnit.Framework;

#pragma warning disable IDE0059 // Unnecessary assignment of a value

namespace ScratchPad
{
    [TestFixture, NUnit.Framework.Category("Performance"), Serial] public class TypeExploration
    {
        List<Type> _assemblyTypes;
        List<Type> _mappableTypes;
        TypeMapper _typeMapper;

        [OneTimeSetUp] public void SetupTask()
        {
            _assemblyTypes = Seq.OfTypes<AccountManagement.API.AccountApi, AccountManagement.AccountManagementServerDomainBootstrapper, AccountManagement.Domain.Events.AccountEvent.Root>()
                                   .SelectMany(type => type.Assembly.GetTypes())
                                   .ToList();

            _mappableTypes = _assemblyTypes.Where(type => !type.IsAbstract || (typeof(BusApi.Remotable.IEvent).IsAssignableFrom(type))).ToList();

            _assemblyTypes.ForEach(type => TypeIndex.For(type));

            _typeMapper = new TypeMapper(new LazySqlServerConnectionProvider(() => "unused"));

            var mapMethod = typeof(TypeMapper).GetMethod(nameof(TypeMapper.Map), new[]{typeof(Guid)});
            foreach(var assemblyType in _mappableTypes)
            {
                mapMethod.MakeGenericMethod(assemblyType).Invoke(_typeMapper, new object[]{Guid.NewGuid()});
            }
        }

        [Test] public void Looking_up_type_associated_data_via_static_caching_trick_is_4_times_faster_than_via_dictionary()
        {
            var iterations = 1_000_000.InstrumentationSlowdown(10);

            var dictionaryTotal = TimeAsserter.Execute(() => DoDictionaryTypeLookups<Email>(iterations)).Total;

            var maxTotal = dictionaryTotal.DivideBy(4).InstrumentationSlowdown(3);
            TimeAsserter.Execute(() => DoStaticTypeLookups<Email>(iterations), maxTotal: maxTotal, maxTries: 1);
        }

        [Test] public void Looking_up_type_by_index_is_25_times_faster_than_through_type_id()
        {
            var iterations = 1_000_000.InstrumentationSlowdown(10);

            var totalViaParsing = TimeAsserter.Execute(() => LookupTypeByParsingTypeId<Email>(iterations), maxTries: 1).Total;
            TimeSpan maxTotal = totalViaParsing.DivideBy(25);
            TimeAsserter.Execute(() => LookupTypeByIndex<Email>(iterations), maxTotal: maxTotal.InstrumentationSlowdown(3.2));

        }

        void LookupTypeByParsingTypeId<TType>(int iterations)
        {
            var typeId = _typeMapper.GetId(typeof(TType));
            var typeIdGuidByteArray = typeId.GuidValue.ToByteArray();
            for(int i = 0; i < iterations; i++)
            {
                var parsedTypeId = new TypeId(new Guid(typeIdGuidByteArray));
                // ReSharper disable once UnusedVariable
                var type = _typeMapper.GetType(parsedTypeId);
            }
        }

        static void LookupTypeByIndex<TType>(int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                var index = TypeIndex.ForService<TType>.Index;
                // ReSharper disable once UnusedVariable
                var type = TypeIndex.BackMap[index];
            }
        }

        static void DoStaticTypeLookups<TType>(int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                // ReSharper disable once UnusedVariable
                var index = TypeIndex.ForService<TType>.Index;
            }
        }

        static void DoDictionaryTypeLookups<TType>(int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                // ReSharper disable once UnusedVariable
                var index = TypeIndex.For(typeof(TType));
            }
        }

        internal static class TypeIndex
        {
            internal static int ServiceCount { get; private set; }
            internal static Dictionary<Type, int> Map = new Dictionary<Type, int>();
            internal static Type[] BackMap = new Type[0];

            internal static int For(Type type)
            {
                if(Map.TryGetValue(type, out var value))
                    return value;

                lock(Map)
                {
                    if(Map.TryGetValue(type, out var value2))
                        return value2;

                    var newMap = new Dictionary<Type, int>(Map) {{type, ServiceCount++}};
                    Map = newMap;

                    var newBackMap = new Type[BackMap.Length + 1];
                    Array.Copy(BackMap, newBackMap, BackMap.Length);
                    newBackMap[^1] = type;
                    BackMap = newBackMap;
                    return ServiceCount - 1;
                }
            }

            internal static class ForService<TType>
            {
                internal static readonly int Index = TypeIndex.For(typeof(TType));
            }
        }
    }
}
