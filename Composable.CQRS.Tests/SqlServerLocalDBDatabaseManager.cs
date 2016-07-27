using System;
using System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests
{
    namespace LocalDBManagerTests
    {
        [TestFixture]
        public class After_creating_two_databases_named_db1_and_db2
        {
            private string _masterConnectionString;
            private SqlServerConnectionUtilities _masterConnection;
            private TemporaryLocalDbManager _manager;
            private string _dB1ConnectionString;
            private string _dB2ConnectionString;
            private string _dB2DbName;
            private string _dB1DbName;
            private const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
            private const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";


            [SetUp]
            public void SetupTask()
            {
                _masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;
                _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);
                _manager = new TemporaryLocalDbManager(_masterConnectionString);
                _dB1ConnectionString = _manager.CreateOrGetLocalDb(Db1);
                _dB2ConnectionString = _manager.CreateOrGetLocalDb(Db2);

                _dB1DbName = new SqlServerConnectionUtilities(_dB1ConnectionString).UseConnection(connection => connection.Database);
                _dB2DbName = new SqlServerConnectionUtilities(_dB2ConnectionString).UseConnection(connection => connection.Database);
            }

            [Test]
            public void Connection_to_Db1_can_be_opened_and_used()
            {
                new SqlServerConnectionUtilities(_manager.CreateOrGetLocalDb(Db1)).ExecuteScalar("select 1")
                    .Should().Be(1);
            }

            [Test]
            public void Connection_to_Db2_can_be_opened_and_used()
            {
                new SqlServerConnectionUtilities(_manager.CreateOrGetLocalDb(Db2)).ExecuteScalar("select 1")
                    .Should().Be(1);
            }

            [Test]
            public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1()
            {
                _manager.CreateOrGetLocalDb(Db1)
                        .Should().Be(_dB1ConnectionString);
            }

            [Test]
            public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2()
            {
                _manager.CreateOrGetLocalDb(Db2)
                        .Should().Be(_dB2ConnectionString);
            }

            [Test]
            public void The_Db1_connectionstring_is_different_from_the_Db2_connection_string()
            {
                _dB1ConnectionString.Should().NotBe(_dB2ConnectionString);
            }

            [TearDown]
            public void TearDownTask()
            {
                _manager.Dispose();

                _manager.Invoking(man => man.CreateOrGetLocalDb(Db1))
                        .ShouldThrow<Exception>()
                        .Where(exception => exception.Message.ToLower().Contains("disposed"));

                ((int)_masterConnection.ExecuteScalar($"select count(*) from sys.databases where name = '{_dB1DbName}'"))
                    .Should().Be(0, "The Db1 database should be removed after disposing the manager");

                ((int)_masterConnection.ExecuteScalar($"select count(*) from sys.databases where name = '{_dB2DbName}'"))
                    .Should().Be(0, "The Db2 database should be removed after disposing the manager");
            }
        }        
    }
}