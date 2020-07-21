using System;
using Composable.DependencyInjection;
using Composable.System.Configuration;
using Composable.System.Reflection;
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

                    return GetConfigurationFileSpecifiedProvider();
                }
            }

            static DependencyInjection.PersistenceLayer GetConfigurationFileSpecifiedProvider()
            {
                const string composableTestingPersistenceLayerParameterName = "Composable.Testing.PersistenceLayer";

                var configurationReader = new AppSettingsJsonConfigurationParameterProvider();
                var configurationValue = configurationReader.GetString(composableTestingPersistenceLayerParameterName);

                if(!Enum.TryParse(configurationValue, out DependencyInjection.PersistenceLayer persistenceLayer))
                {
                    throw new Exception($"The configuration parameter:{composableTestingPersistenceLayerParameterName} has an invalid value: {configurationValue}. It must be one of the values in: {typeof(DependencyInjection.PersistenceLayer).GetFullNameCompilable()}.");
                }
                return persistenceLayer;
            }

            static DependencyInjection.PersistenceLayer GetNCrunchProvider()
            {
                var storageProviderName = NCrunchEnvironment.GetDuplicatedDimension();
                if(!Enum.TryParse(storageProviderName, out DependencyInjection.PersistenceLayer provider))
                {
                    throw new Exception($"Failed to parse PersistenceLayerProvider from test environment. Value was: {storageProviderName}");
                }

                return provider;
            }

            public static TValue ValueFor<TValue>(TValue msSql= default, TValue mySql = default, TValue inMem = default, TValue pgSql = default, TValue orcl = default, TValue db2 = default)
                =>
                    Current switch
                {
                    DependencyInjection.PersistenceLayer.MsSql => SelectValue(msSql, nameof(msSql)),
                    DependencyInjection.PersistenceLayer.Memory => SelectValue(inMem, nameof(inMem)),
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
