using Composable.DependencyInjection;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions.Extensions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.KeyValueStorage
{
    [Performance, LongRunning, Serial]
    [TestFixture] class DocumentDbPerformanceTests : DocumentDbTestsBase
    {
        [Test] public void In150Milliseconds_MsSql_saves_200_MySql_55_InMemory_2000()
        {
            ServiceLocator.ExecuteInIsolatedScope(() =>
            {
                var updater = ServiceLocator.DocumentDbUpdater();

                void SaveOneNewUserInTransaction()
                {
                    var user = new User();
                    updater.Save(user);
                }

                //Warm up caches etc
                SaveOneNewUserInTransaction();

                //Urgent: Fix the MySql opening connection slowness problem and up the number for MySql in this test
                TimeAsserter.Execute(
                    action: SaveOneNewUserInTransaction,
                    iterations: TestEnvironment.ValueForPersistenceProvider(msSql:200, mySql:55, inMem:2000),
                    maxTotal: 150.Milliseconds()
                );
            });
        }
    }
}
