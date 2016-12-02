using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.Testing;
using Composable.System.Transactions;

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
            ReleaseOldLocks();
        }

        public void RegisterWithContainer(IWindsorContainer container)
        {
            container.Register(Component.For<TemporaryLocalDbManager>().UsingFactoryMethod(() => this));//Register and resolve instance once so that it is disposed with the container
            container.Resolve<TemporaryLocalDbManager>();
        }

        private readonly Dictionary<string, ManagedLocalDb> _reservedDatabases = new Dictionary<string, ManagedLocalDb>();
        private bool _disposed;

        public string CreateOrGetLocalDb(string requestedDbName)
        {
            Contract.Assert(!_disposed, "Attempt to use disposed object");
            bool needsToCleanDb = false;
            string dbName=null;
            if(!_reservedDatabases.ContainsKey(requestedDbName))
            {
                using(var transaction = new TransactionScope())
                {
                    if(TryReserveDatabase(out dbName))
                    {
                        _reservedDatabases.Add(
                            requestedDbName,
                            new ManagedLocalDb(name: dbName, connectionString: ConnectionStringForDbNamed(dbName)));
                        needsToCleanDb = true;
                    }
                    else
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        dbName = $"TemporaryLocalDbManager_{Guid.NewGuid()}.mdf";

                        using(new TransactionScope(TransactionScopeOption.Suppress))
                        {
                            _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{dbName}]");
                        }

                        InsertDatabase(dbName);

                        _reservedDatabases.Add(
                            requestedDbName,
                            new ManagedLocalDb(name: dbName, connectionString: ConnectionStringForDbNamed(dbName)));
                    }
                    transaction.Complete();
                }
            }

            if(needsToCleanDb)
            {
                EmptyOutDatase(dbName);
            }

            return _reservedDatabases[requestedDbName].ConnectionString;
        }

        private static readonly HashSet<string> ConnectionStringsWithKnownManagerDb = new HashSet<string>();
        private bool ManagerDbExists()
        {
            if(!ConnectionStringsWithKnownManagerDb.Contains(_masterConnectionString))
            {
                if(_masterConnection.ExecuteScalar($"select DB_ID('{ManagerDbName}')") == DBNull.Value)
                {
                    return false;
                }
            }

            ConnectionStringsWithKnownManagerDb.Add(_masterConnectionString);
            return true;
        }

        private void CreateManagerDB()
        {
            lock(typeof(TemporaryLocalDbManager))
            {
                if (!ManagerDbExists())
                {
                    _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{ManagerDbName}]");
                    _managerConnection.ExecuteNonQuery(CreateDbTableSql);
                    ConnectionStringsWithKnownManagerDb.Add(_masterConnectionString);
                }
            }
        }

        private static class ManagerTableSchema
        {
            public static readonly string TableName = "Databases";
            public static readonly string DatabaseName = nameof(DatabaseName);
            public static readonly string IsFree = nameof(IsFree);
            public static readonly string ReservationDate = nameof(ReservationDate);
        }

        private static readonly string CreateDbTableSql = $@"
CREATE TABLE [dbo].[{ManagerTableSchema.TableName}](
	[{ManagerTableSchema.DatabaseName}] [varchar](500) NOT NULL,
	[{ManagerTableSchema.IsFree}] [bit] NOT NULL,
    [{ManagerTableSchema.ReservationDate}] [datetime] NOT NULL
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
            _managerConnection.ExecuteNonQuery($"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 0, {ManagerTableSchema.ReservationDate} = getdate() where {ManagerTableSchema.DatabaseName} = '{dbName}'");
        }

        private void EmptyOutDatase(string dbName)
        {
            new SqlServerConnectionUtilities(ConnectionStringForDbNamed(dbName))
                .UseConnection(connection => connection.DropAllObjects());
        }

        private void InsertDatabase(string dbName)
        {
            _managerConnection.ExecuteNonQuery(
                $"insert {ManagerTableSchema.TableName} ({ManagerTableSchema.DatabaseName}, {ManagerTableSchema.IsFree}, {ManagerTableSchema.ReservationDate}) values('{dbName}', 0, getdate())");
        }

        private void ReleaseDatabase(ManagedLocalDb managedLocalDb)
        {
            _reservedDatabases.Remove(managedLocalDb.Name);
            _managerConnection.ExecuteNonQuery($"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 1 where {ManagerTableSchema.DatabaseName} = '{managedLocalDb.Name}'");            
        }

        private IEnumerable<string> FreeDatabases()
        {            
            return _managerConnection.UseCommand(
                command =>
                {
                    var names = new List<string>();
                    command.CommandText = $"select {ManagerTableSchema.DatabaseName}, {ManagerTableSchema.IsFree} from {ManagerTableSchema.TableName} With(TABLOCKX)";
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            var dbName = reader.GetString(0);
                            var isFree = reader.GetBoolean(1);
                            if(isFree)
                            {                                
                                names.Add(dbName);
                            }
                        }
                    }
                    return names;
                });
        }

        private void ReleaseOldLocks()
        {
            _managerConnection.ExecuteNonQuery(
                $"update {ManagerTableSchema.TableName} With(TABLOCKX) set {ManagerTableSchema.IsFree} = 1 where {ManagerTableSchema.ReservationDate} < dateadd(minute, -60, getdate())");
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
                InTransaction.Execute(() => _reservedDatabases.Values.ForEach(ReleaseDatabase));
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

        public void RemoveAllDatabases()
        {
            var dbsToDrop = new List<string>();
            _masterConnection.UseCommand(
                command =>
                {
                    command.CommandText = "select name from sysdatabases";
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            var dbName = reader.GetString(0);
                            if((dbName.StartsWith(ManagerDbName) && dbName != ManagerDbName))
                            {
                                dbsToDrop.Add(dbName);
                            }
                        }
                    }
                });

            foreach(var db in dbsToDrop)
            {
                var dropCommand = $"drop database [{db}]";
                Debug.WriteLine(dropCommand);
                try
                {
                    _masterConnection.ExecuteNonQuery(dropCommand);
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }

            _managerConnection.ExecuteNonQuery($"delete {ManagerTableSchema.TableName}");
        }
    }
}