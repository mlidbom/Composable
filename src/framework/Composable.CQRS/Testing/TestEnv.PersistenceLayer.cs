﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Reflection;
using NCrunch.Framework;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Composable.Testing
{
    ///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
    static partial class TestEnv
    {
        ///<summary>Persistence layer members</summary>
        internal static class PersistenceLayer
        {
            public static DependencyInjection.PersistenceLayer Current => GetNCrunchProvider();

            static DependencyInjection.PersistenceLayer GetNCrunchProvider()
            {
                var testName = TestContext.CurrentContext.Test.FullName;
                var match = FindDimensions.Match(testName);

                var storageProviderName = match.Groups[1].Value;
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

        static readonly Regex FindDimensions = new Regex(@"\(""(.*)\:(.*)""\)", RegexOptions.Compiled);
        internal static class DIContainer
        {
            internal static DependencyInjection.DIContainer Current => GetNCrunchConfiguredDiContainer();

            static DependencyInjection.DIContainer GetNCrunchConfiguredDiContainer()
            {
                var testName = TestContext.CurrentContext.Test.FullName;
                var match = FindDimensions.Match(testName);

                var containerName = match.Groups[2].Value;
                if (!Enum.TryParse(containerName, out DependencyInjection.DIContainer provider))
                {
                    throw new Exception($"Failed to parse DIContainer from test environment. Value was: {containerName}");
                }

                return provider;
            }
        }
    }
}