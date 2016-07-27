using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.Testing;

namespace CQRS.Tests
{
    public class TemporaryLocalDbManager : IDisposable
    {
        private readonly string _masterConnectionString;
        private readonly SqlServerConnectionUtilities _masterConnection;
        private readonly SqlServerConnectionUtilities _managerConnection;

        private static readonly string ManagerDbName = $"{nameof(TemporaryLocalDbManager)}";

        public TemporaryLocalDbManager(string masterConnectionString, IWindsorContainer container = null)
        {
            _masterConnectionString = masterConnectionString; 
            _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);
            if(container != null)
            {
                RegisterWithContainer(container);
            }

            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString);
            sqlConnectionStringBuilder.InitialCatalog = ManagerDbName;
            _managerConnection = new SqlServerConnectionUtilities(sqlConnectionStringBuilder.ConnectionString);

            CreateManagerDB();
        }

        public void RegisterWithContainer(IWindsorContainer container)
        {
            container.Register(Component.For<TemporaryLocalDbManager>().UsingFactoryMethod(() => this));//Register and resolve instance once so that it is disposed with the container
            container.Resolve<TemporaryLocalDbManager>();
        }

        private static readonly string DbDirectory = $"{nameof(TemporaryLocalDbManager)}_Databases";

        private readonly Dictionary<string, ManagedLocalDb> _reservedDatabases = new Dictionary<string, ManagedLocalDb>();
        private bool _disposed;        

        public string CreateOrGetLocalDb(string requestedDbName)
        {
            using(var transaction = new TransactionScope())
            {
                Contract.Assert(!_disposed, "Attempt to use disposed object");
                if(!_reservedDatabases.ContainsKey(requestedDbName))
                {
                    string dbName;
                    if(TryReserveDatabase(out dbName))
                    {
                        _reservedDatabases.Add(
                            requestedDbName,
                            new ManagedLocalDb(name: dbName, connectionString: ConnectionStringForDbNamed(dbName)));
                    }
                    else
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DbDirectory);
                        dbName = $"TemporaryLocalDbManager_{Guid.NewGuid()}.mdf";
                        var dbFullFileName = Path.Combine(outputFolder, dbName);
                        if(!Directory.Exists(outputFolder))
                        {
                            Directory.CreateDirectory(outputFolder);
                        }

                        _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{dbName}] ON (NAME = N'{dbName}', FILENAME = '{dbFullFileName}')");

                        InsertDatabase(dbName);

                        _reservedDatabases.Add(requestedDbName,new ManagedLocalDb(name: dbName, connectionString: ConnectionStringForDbNamed(dbName)));
                    }
                }

                transaction.Complete();
                return _reservedDatabases[requestedDbName].ConnectionString;
            }
        }

        private bool ManagerDbExists()
        {
            return (int)_masterConnection.ExecuteScalar($"select count(*) from sys.databases where name = '{ManagerDbName}'") == 1;
        }

        private void CreateManagerDB()
        {
            if(!ManagerDbExists())
            {
                _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{ManagerDbName}]");
                _managerConnection.ExecuteNonQuery(CreateDbTableSql);
            }
        }

        private static class ManagerTableSchema
        {
            public static readonly string TableName = "Databases";
            public static readonly string DatabaseName = nameof(DatabaseName);
            public static readonly string IsFree = nameof(IsFree);
        }

        private static readonly string CreateDbTableSql = $@"
CREATE TABLE [dbo].[{ManagerTableSchema.TableName}](
	[{ManagerTableSchema.DatabaseName}] [varchar](500) NOT NULL,
	[{ManagerTableSchema.IsFree}] [bit] NOT NULL,
 CONSTRAINT [PK_DataBases] PRIMARY KEY CLUSTERED 
(
	[{ManagerTableSchema.DatabaseName}] ASC
))
";

        private string ConnectionStringForDbNamed(string dbName)
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString);
            sqlConnectionStringBuilder.InitialCatalog = dbName;
            return sqlConnectionStringBuilder.ConnectionString;
        }

        private bool TryReserveDatabase(out string databaseName)
        {

            databaseName = null;
            var freeDbs = FreeDatabases();
            if(freeDbs.Any())
            {
                databaseName = freeDbs.First();
                ReserveDatabase(databaseName);
                return true;
            }
            return false;
        }

        private void ReserveDatabase(string dbName)
        {
            Console.WriteLine($"Reserving DB {dbName}");
            _managerConnection.ExecuteNonQuery($"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 0 where {ManagerTableSchema.DatabaseName} = '{dbName}'");
        }

        private void InsertDatabase(string dbName)
        {
            _managerConnection.ExecuteNonQuery($"insert {ManagerTableSchema.TableName} ({ManagerTableSchema.DatabaseName}, {ManagerTableSchema.IsFree}) values('{dbName}', 0)");
        }

        private void ReleaseDatabase(ManagedLocalDb managedLocalDb)
        {
            _reservedDatabases.Remove(managedLocalDb.Name);
            new SqlServerConnectionUtilities(ConnectionStringForDbNamed(managedLocalDb.Name))
                .UseConnection(connection => connection.DropAllObjects());

            using (var conn = new SqlConnection(managedLocalDb.ConnectionString))
            {
                SqlConnection.ClearPool(conn);
            }

            _managerConnection.ExecuteNonQuery($"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 1 where {ManagerTableSchema.DatabaseName} = '{managedLocalDb.Name}'");
        }

        private IEnumerable<string> FreeDatabases()
        {
            return _managerConnection.UseCommand(
                command =>
                {
                    var names = new List<string>();
                    command.CommandText = $"select {ManagerTableSchema.DatabaseName} from {ManagerTableSchema.TableName} where {ManagerTableSchema.IsFree} = 1";
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            names.Add(reader.GetString(0));
                        }
                    }
                    return names;
                });
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
                _reservedDatabases.Values.ForEach(ReleaseDatabase);
                _disposed = true;
            }
        }

        ~TemporaryLocalDbManager()
        {
            InternalDispose();
        }

        private class ManagedLocalDb
        {
            public string Name { get; }
            public string ConnectionString { get; }
            public ManagedLocalDb(string name, string connectionString)
            {
                Name = name;
                ConnectionString = connectionString;
            }
        }
    }
}