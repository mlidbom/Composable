using System;
using System.Configuration;
using Composable.Testing.System.Data.SqlClient;
using Composable.Testing.Testing.Databases;
using FluentAssertions;
using Xunit;

namespace Composable.Testing.Tests.Databases.SqlServerDatabasePoolTests
{
    public class After_creating_two_databases_named_db1_and_db2 : IDisposable
    {
        SqlServerDatabasePool _manager;
        ISqlConnection _dB1ConnectionString;
        ISqlConnection _dB2ConnectionString;
        const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
        const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";


        public After_creating_two_databases_named_db1_and_db2()
        {
            var masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"]
                                                                 .ConnectionString;

            _manager = new SqlServerDatabasePool(masterConnectionString);
            _dB1ConnectionString = _manager.ConnectionProviderFor(Db1);
            _dB2ConnectionString = _manager.ConnectionProviderFor(Db2);
        }

        [Fact] public void Connection_to_Db1_can_be_opened_and_used()
        {
            _manager.ConnectionProviderFor(Db1)
                    .ExecuteScalar("select 1")
                    .Should()
                    .Be(1);
        }

        [Fact] public void Connection_to_Db2_can_be_opened_and_used()
        {
            _dB2ConnectionString.ExecuteScalar("select 1")
                                .Should()
                                .Be(1);
        }

        [Fact] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1()
        {
            _manager.ConnectionProviderFor(Db1)
                    .ConnectionString
                    .Should()
                    .Be(_dB1ConnectionString.ConnectionString);
        }

        [Fact] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2()
        {
            _manager.ConnectionProviderFor(Db2)
                    .ConnectionString
                    .Should()
                    .Be(_dB2ConnectionString.ConnectionString);
        }

        [Fact] public void The_Db1_connectionstring_is_different_from_the_Db2_connection_string()
        {
            _dB1ConnectionString.ConnectionString.Should()
                                .NotBe(_dB2ConnectionString.ConnectionString);
        }

        public void Dispose()
        {
            _manager.Dispose();

            _manager.Invoking(man => man.ConnectionProviderFor(Db1))
                    .ShouldThrow<Exception>()
                    .Where(exception => exception.Message.ToLower()
                                                 .Contains("disposed"));
        }
    }
}
