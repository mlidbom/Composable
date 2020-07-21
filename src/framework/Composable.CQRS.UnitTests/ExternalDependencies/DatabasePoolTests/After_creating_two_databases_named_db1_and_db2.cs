using System;
using Composable.DependencyInjection;
using Composable.Testing;
using Composable.Testing.Databases;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests
{
    public class After_creating_two_databases_named_db1_and_db2 : DatabasePoolTest
    {
        DatabasePool _manager;
        const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
        const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";

        [SetUp] public void SetupTask()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
            _manager = CreatePool();
        }

        [Test] public void Connection_to_Db1_can_be_opened_and_used()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            UseConnection(Db1,
                          _manager,
                          connection =>
                          {
                              using var command = connection.CreateCommand();
                              command.CommandText = LayerSpecificCommandText();
                              command.ExecuteScalar().Should().Be(1);
                          });
        }

        [Test] public void Connection_to_Db2_can_be_opened_and_used()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            UseConnection(Db2,
                          _manager,
                          connection =>
                          {
                              using var command = connection.CreateCommand();
                              command.CommandText = LayerSpecificCommandText();
                              command.ExecuteScalar().Should().Be(1);
                          });
        }

        static string LayerSpecificCommandText() => TestEnv.PersistenceLayer.ValueFor(msSql:"select 1", mySql:"select 1", pgSql: "select 1", orcl: "select 1 from dual", db2:"select 1 from sysibm.sysdummy1");

        [Test] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            _manager.ConnectionStringFor(Db1).Should().Be(_manager.ConnectionStringFor(Db1));
        }

        [Test] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            _manager.ConnectionStringFor(Db2).Should().Be(_manager.ConnectionStringFor(Db2));
        }

        [Test] public void The_Db1_connectionstring_is_different_from_the_Db2_connection_string()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            _manager.ConnectionStringFor(Db1).Should().NotBe(_manager.ConnectionStringFor(Db2));
        }

        [TearDown] public void TearDownTask()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
            _manager.Dispose();

            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            _manager.Invoking(action: man => _manager.ConnectionStringFor(Db1))
                    .Should().Throw<Exception>()
                    .Where(exceptionExpression: exception => exception.Message.ToLowerInvariant()
                                                                      .Contains("disposed"));
        }

        public After_creating_two_databases_named_db1_and_db2(string _) : base(_) {}
    }
}
