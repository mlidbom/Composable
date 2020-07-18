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


        public static TValue ValueForPersistenceProvider<TValue>(TValue msSql= default, TValue mySql = default, TValue inMem = default, TValue pgSql = default, TValue orcl = default, TValue db2 = default)
            => TestEnvironment.TestingPersistenceLayer switch
            {
                PersistenceLayer.MsSql => SelectValue(msSql, nameof(msSql)),
                PersistenceLayer.InMemory => SelectValue(inMem, nameof(inMem)),
                PersistenceLayer.MySql => SelectValue(mySql, nameof(mySql)),
                PersistenceLayer.PgSql => SelectValue(pgSql, nameof(pgSql)),
                PersistenceLayer.Orcl => SelectValue(orcl, nameof(orcl)),
                PersistenceLayer.DB2 => SelectValue(db2, nameof(db2)),
                _ => throw new ArgumentOutOfRangeException()
            };

        static TValue SelectValue<TValue>(TValue value, string provider)
        {
            if(!Equals(value, default(TValue))) return value;

            throw  new Exception($"Value missing for {provider}");
        }
    }
}
