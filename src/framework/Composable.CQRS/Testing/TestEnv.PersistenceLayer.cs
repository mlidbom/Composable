using System;
using System.Linq;
using System.Text.RegularExpressions;
using Composable.System;

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
                    var storageProviderName = FindDimensions.Match(GetTestName()).Groups[1].Value;
                    if(Enum.TryParse(storageProviderName, out DependencyInjection.PersistenceLayer provider)) return provider;

                    throw new Exception($"Failed to parse PersistenceLayerProvider from test environment. Value was: {storageProviderName}");
                }
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

        static string GetTestName()
        {
            //We do not want to reference NUnit so dig this data out through reflection. When running tests NUnit will be there.
            var currentContext = TestContextType.GetProperty("CurrentContext")!.GetMethod!.Invoke(null, null)!;
            var test = currentContext.GetType().GetProperty("Test")!.GetMethod!.Invoke(currentContext, null)!;
            var testName = (string)test.GetType().GetProperty("FullName")!.GetMethod!.Invoke(test, null)!;
            return testName;
        }

        static readonly Type TestContextType = AppDomain.CurrentDomain
                                                        .GetAssemblies()
                                                        .Single(ass => ass.GetName().FullName.ContainsInvariant("nunit.framework"))
                                                        .GetType("NUnit.Framework.TestContext")!;

        static readonly Regex FindDimensions = new Regex(@"\(""(.*)\:(.*)""\)", RegexOptions.Compiled);
        internal static class DIContainer
        {
            internal static DependencyInjection.DIContainer Current
            {
                get
                {
                    var containerName = FindDimensions.Match(GetTestName()).Groups[2].Value;
                    if (!Enum.TryParse(containerName, out DependencyInjection.DIContainer provider))
                    {
                        throw new Exception($"Failed to parse DIContainer from test environment. Value was: {containerName}");
                    }

                    return provider;
                }
            }
        }
    }
}
