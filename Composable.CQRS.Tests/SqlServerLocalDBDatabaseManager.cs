using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using Castle.Core.Internal;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests
{
    public class TemporaryLocalDBManager : IDisposable
    {
        private readonly string _masterConnectionString;
        private readonly SqlServerConnectionUtilities _masterConnection; 

        public TemporaryLocalDBManager(string masterConnectionString)
        {
            _masterConnectionString = masterConnectionString; 
            _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);
        }

        private static readonly string DbDirectory = $"{nameof(TemporaryLocalDBManager)}_Databases";

        private readonly Dictionary<string, ManagedLocalDb> _managedDatabases = new Dictionary<string, ManagedLocalDb>();
        private bool _disposed;        

        public string CreateOrGetLocalDb(string dbName)
        {
            Contract.Assert(!_disposed, "Attempt to use disposed object");
            if (!_managedDatabases.ContainsKey(dbName))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DbDirectory);
                var dbFileName = $"{dbName}_{Guid.NewGuid()}.mdf";
                var dbFullFileName = Path.Combine(outputFolder, dbFileName);
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{dbFileName}] ON (NAME = N'{dbFileName}', FILENAME = '{dbFullFileName}')");

                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString);
                sqlConnectionStringBuilder.InitialCatalog = dbFileName;

                _managedDatabases.Add(dbName, new ManagedLocalDb(name: dbFileName, fileName: dbFullFileName, connectionString: sqlConnectionStringBuilder.ConnectionString));
            }

            return _managedDatabases[dbName].ConnectionString;
        }

        private void DropDatabase(ManagedLocalDb db)
        {
            using(var conn = new SqlConnection(db.ConnectionString))
            {
                SqlConnection.ClearPool(conn);
            }
            
            _masterConnection.ExecuteNonQuery($@"drop database [{db.Name}]");
        }

        public void Dispose()
        {
            InternalDispose();
            GC.SuppressFinalize(this);
        }

        protected virtual void InternalDispose()
        {
            if (!_disposed)
            {
                _managedDatabases.Values.ForEach(DropDatabase);
                _disposed = true;
            }
        }

        ~TemporaryLocalDBManager()
        {
            InternalDispose();
        }

        private class ManagedLocalDb
        {
            public string Name { get; }
            public string FileName { get; }
            public string ConnectionString { get; }
            public ManagedLocalDb(string name, string fileName, string connectionString)
            {
                Name = name;
                FileName = fileName;
                ConnectionString = connectionString;
            }
        }
    }

    namespace LocalDBManagerTests
    {
        [TestFixture]
        public class After_creating_two_databases_named_db1_and_db2
        {
            private string _masterConnectionString;
            private SqlServerConnectionUtilities _masterConnection;
            private TemporaryLocalDBManager _manager;
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
                _manager = new TemporaryLocalDBManager(_masterConnectionString);
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