using System;
using Composable.DependencyInjection;
using NCrunch.Framework;

namespace Composable.Testing
{
    ///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
    static partial class TestEnv
    {
        ///<summary>Persistence layer members</summary>
        internal static class PersistenceLayer
        {
            public static DependencyInjection.PersistenceLayer Current
            {
                get
                {
                    if(IsRunningUnderNCrunch) return GetNCrunchProvider();

                    if(TryGetNunitParameterForProvider(out DependencyInjection.PersistenceLayer nUnitPersistenceLayer)) return nUnitPersistenceLayer;

                    if(TryGetConfigurationFileSpecifiedProvider(out DependencyInjection.PersistenceLayer configurationFilePersistenceLayer)) return configurationFilePersistenceLayer;

                    return DependencyInjection.PersistenceLayer.InMemory;
                }
            }

            static bool TryGetConfigurationFileSpecifiedProvider(out DependencyInjection.PersistenceLayer persistenceLayer)
            {
                //urgent: implement
                persistenceLayer = (DependencyInjection.PersistenceLayer) (-1);
                return false;
            }

            static bool TryGetNunitParameterForProvider(out DependencyInjection.PersistenceLayer persistenceLayer)
            {
                //urgent:implement
                persistenceLayer = (DependencyInjection.PersistenceLayer) (-1);
                return false;
            }

            static DependencyInjection.PersistenceLayer GetNCrunchProvider()
            {
                var storageProviderName = NCrunchEnvironment.GetDuplicatedDimension();
                if(!Enum.TryParse(storageProviderName, out DependencyInjection.PersistenceLayer provider))
                {
                    throw new Exception("Failed to parse PersistenceLayerProvider from test environment");
                }

                return provider;
            }

            public static TValue ValueFor<TValue>(TValue msSql= default, TValue mySql = default, TValue inMem = default, TValue pgSql = default, TValue orcl = default, TValue db2 = default)
                =>
                    Current switch
                {
                    DependencyInjection.PersistenceLayer.MsSql => SelectValue(msSql, nameof(msSql)),
                    DependencyInjection.PersistenceLayer.InMemory => SelectValue(inMem, nameof(inMem)),
                    DependencyInjection.PersistenceLayer.MySql => SelectValue(mySql, nameof(mySql)),
                    DependencyInjection.PersistenceLayer.PgSql => SelectValue(pgSql, nameof(pgSql)),
                    DependencyInjection.PersistenceLayer.Orcl => SelectValue(orcl, nameof(orcl)),
                    DependencyInjection.PersistenceLayer.DB2 => SelectValue(db2, nameof(db2)),
                    _ => throw new ArgumentOutOfRangeException()
                };

            static TValue SelectValue<TValue>(TValue value, string provider)
            {
                if(!Equals(value, default(TValue))) return value;

                throw  new Exception($"Value missing for {provider}");
            }
        }
    }
}
