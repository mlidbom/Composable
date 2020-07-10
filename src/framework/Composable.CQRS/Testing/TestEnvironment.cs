using System;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Diagnostics;
using NCrunch.Framework;

namespace Composable.Testing
{
    static class TestEnvironment
    {
        public static TimeSpan InstrumentationSlowdown(this TimeSpan original, double slowdownFactor)
        {
            if(IsInstrumented)
            {
                return ((int)(original.TotalMilliseconds * slowdownFactor)).Milliseconds();
            } else
            {
                return original;
            }
        }

        public static int InstrumentationSlowdown(this int original, double slowdownFactor)
        {
            if(IsInstrumented)
            {
                return (int)(original / slowdownFactor);
            } else
            {
                return original;
            }
        }

        static readonly bool IsInstrumented = CheckIfInstrumented();
        static bool CheckIfInstrumented()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(!IsRunningUnderNCrunch) return false;

            var time = StopwatchExtensions.TimeExecution(() =>
                                                         {
                                                             for(int i = 0; i < 100; i++)
                                                             {
                                                                 // ReSharper disable once UnusedVariable
                                                                 int something = i;
                                                             }
                                                         },
                                                         iterations: 500);

            return time.Total.TotalMilliseconds > 1;
        }

        static bool IsRunningUnderNCrunch
        {
            get
            {
#if NCRUNCH
    return true;
#else
    return false;
#endif
            }
        }

        public static PersistenceLayer TestingPersistenceLayer
        {
            get
            {
                var storageProviderName = NCrunchEnvironment.GetDuplicatedDimension();
                if(!Enum.TryParse(storageProviderName, out PersistenceLayer provider))
                {
                    throw new Exception("Failed to parse PersistenceLayerProvider from test environment");
                }

                return provider;
            }
        }


        public static int ValueForPersistenceProvider(int? msSql = null, int? mySql = null, int? inMem = null, int? pgSql = null)
            => ValueForPersistenceProvider<int>(msSql: msSql, mySql: mySql, inMem: inMem, pgSql: pgSql);

        public static TValue ValueForPersistenceProvider<TValue>(TValue? msSql= null, TValue? mySql = null, TValue? inMem = null, TValue? pgSql = null)
        where TValue : struct
        => TestEnvironment.TestingPersistenceLayer switch
            {
                PersistenceLayer.MsSql => msSql ?? throw new Exception($"Value missing for {nameof(msSql)}"),
                PersistenceLayer.InMemory => inMem ?? throw new Exception($"Value missing for {nameof(inMem)}"),
                PersistenceLayer.MySql => mySql?? throw new Exception($"Value missing for {nameof(mySql)}"),
                PersistenceLayer.PgSql => pgSql?? throw new Exception($"Value missing for {nameof(pgSql)}"),
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}
