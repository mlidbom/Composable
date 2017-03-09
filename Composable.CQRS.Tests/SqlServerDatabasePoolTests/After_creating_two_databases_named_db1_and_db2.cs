using System;
using System.Configuration;
using Composable.CQRS.Testing;
using Composable.System.Data.SqlClient;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.SqlServerDatabasePoolTests
{
    [TestFixture]
    public class After_creating_two_databases_named_db1_and_db2
    {
        string _masterConnectionString;
        SqlServerConnectionUtilities _masterConnection;
        SqlServerDatabasePool _manager;
        string _dB1ConnectionString;
        string _dB2ConnectionString;
        string _dB2DbName;
        string _dB1DbName;
        const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
        const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";


        [SetUp]
        public void SetupTask()
        {
            _masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;
            _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);
            _manager = new SqlServerDatabasePool(_masterConnectionString);
            _dB1ConnectionString = _manager.ConnectionStringFor(Db1);
            _dB2ConnectionString = _manager.ConnectionStringFor(Db2);

            _dB1DbName = new SqlServerConnectionUtilities(_dB1ConnectionString).UseConnection(connection => connection.Database);
            _dB2DbName = new SqlServerConnectionUtilities(_dB2ConnectionString).UseConnection(connection => connection.Database);
        }

        [Test]
        public void Connection_to_Db1_can_be_opened_and_used()
        {
            new SqlServerConnectionUtilities(_manager.ConnectionStringFor(Db1)).ExecuteScalar("select 1")
                                                                               .Should().Be(1);
        }

        [Test]
        public void Connection_to_Db2_can_be_opened_and_used()
        {
            new SqlServerConnectionUtilities(_manager.ConnectionStringFor(Db2)).ExecuteScalar("select 1")
                                                                               .Should().Be(1);
        }

        [Test]
        public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1()
        {
            _manager.ConnectionStringFor(Db1)
                    .Should().Be(_dB1ConnectionString);
        }

        [Test]
        public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2()
        {
            _manager.ConnectionStringFor(Db2)
                    .Should().Be(_dB2ConnectionString);
        }

        [Test]
        public void The_Db1_connectionstring_is_different_from_the_Db2_connection_string()
        {
            _dB1ConnectionString.Should().NotBe(_dB2ConnectionString);
        }


        [Test]
        public void Six_competing_threads_can_reserve_and_release_100_databases_in_10_seconds()
        {
            TimeAsserter.ExecuteThreaded(action:
                () =>
                {

                }, maxTotal: 10.Seconds());
        }

        [TearDown]
        public void TearDownTask()
        {
            _manager.Dispose();

            _manager.Invoking(man => man.ConnectionStringFor(Db1))
                    .ShouldThrow<Exception>()
                    .Where(exception => exception.Message.ToLower().Contains("disposed"));
        }
    }
}