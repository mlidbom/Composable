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
    public class TestDatabasePool : IDisposable
    {
        private readonly string _masterConnectionString;
        private readonly SqlServerConnectionUtilities _masterConnection;
        private readonly SqlServerConnectionUtilities _managerConnection;

        private static readonly string ManagerDbName = $"{nameof(TestDatabasePool)}";

        public TestDatabasePool(string masterConnectionString, IWindsorContainer container = null)
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
            container.Register(Component.For<TestDatabasePool>().UsingFactoryMethod(() => this));//Register and resolve instance once so that it is disposed with the container
            container.Resolve<TestDatabasePool>();
        }

        private readonly Dictionary<string, Database> _reservedDatabases = new Dictionary<string, Database>();
        private bool _disposed;

        public string ConnectionStringFor(string requestedDbName)
        {
            Contract.Assert(!_disposed, "Attempt to use disposed object");
            Database database;
            if(!_reservedDatabases.TryGetValue(requestedDbName, out database))
            {
                using(var transaction = new TransactionScope())
                {
                    if(TryReserveDatabase(out database))
                    {
                        _reservedDatabases.Add(requestedDbName,database);
                    }
                    else
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var newDBName = $"{ManagerDbName}_{Guid.NewGuid()}.mdf";
                        database = new Database(newDBName, isEmpty:true, isFree:false, reservationDate:DateTime.UtcNow, connectionString: ConnectionStringForDbNamed(newDBName));

                        using(new TransactionScope(TransactionScopeOption.Suppress))
                        {
                            CreateDatabase(database.Name);
                        }

                        InsertDatabase(database.Name);

                        _reservedDatabases.Add(requestedDbName,database);
                    }
                    transaction.Complete();
                }                
            }

            if(!database.IsEmpty)
            {
                EmptyOutDatase(database.Name);
            }

            return _reservedDatabases[requestedDbName].ConnectionString;
        }

        void CreateDatabase(string databaseName)
        {
            _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{databaseName}]");
            _masterConnection.ExecuteNonQuery($"ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;");
            Console.WriteLine($"Created: {databaseName}");
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
            lock(typeof(TestDatabasePool))
            {
                if (!ManagerDbExists())
                {
                    CreateDatabase(ManagerDbName);
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
            public static readonly string IsClean = nameof(IsClean);
            public static readonly string ReservationDate = nameof(ReservationDate);
            public static object ReservationCallStack = nameof(ReservationCallStack);
        }

        private static readonly string CreateDbTableSql = $@"
CREATE TABLE [dbo].[{ManagerTableSchema.TableName}](
	[{ManagerTableSchema.DatabaseName}] [varchar](500) NOT NULL,
	[{ManagerTableSchema.IsFree}] [bit] NOT NULL,
    [{ManagerTableSchema.IsClean}] [bit] NOT NULL,
    [{ManagerTableSchema.ReservationDate}] [datetime] NOT NULL,
    [{ManagerTableSchema.ReservationCallStack}] [varchar](max) NOT NULL,
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

        private bool TryReserveDatabase(out Database databaseName)
        {

            databaseName = null;
            var freeDbs = GetDatabases().Where(db => db.IsFree).ToList();
            if(freeDbs.Any())
            {
                databaseName = freeDbs.First();
                ReserveDatabase(databaseName.Name);
                return true;
            }
            return false;
        }

        private void ReserveDatabase(string dbName)
        {
            _managerConnection.ExecuteNonQuery($"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 0, {ManagerTableSchema.IsClean} = 0, {ManagerTableSchema.ReservationDate} = getdate(), {ManagerTableSchema.ReservationCallStack} = '{Environment.StackTrace}' where {ManagerTableSchema.DatabaseName} = '{dbName}'");
            Console.WriteLine($"Reserved:{dbName}");
        }

        private void EmptyOutDatase(string dbName)
        {
            new SqlServerConnectionUtilities(ConnectionStringForDbNamed(dbName))
                .UseConnection(connection => connection.DropAllObjects());
        }

        private void InsertDatabase(string dbName)
        {
            _managerConnection.ExecuteNonQuery(
                $"insert {ManagerTableSchema.TableName} ({ManagerTableSchema.DatabaseName}, {ManagerTableSchema.IsFree},{ManagerTableSchema.IsClean}, {ManagerTableSchema.ReservationDate},  {ManagerTableSchema.ReservationCallStack}) values('{dbName}', 0, 1, getdate(), '{Environment.StackTrace}')");
        }

        private void ReleaseDatabase(Database database)
        {
            _reservedDatabases.Remove(database.Name);
            Task.Run(() =>
                    {
                        EmptyOutDatase(database.Name);
                        var releasedDBs = _managerConnection.ExecuteNonQuery(
                            $"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 1, {ManagerTableSchema.IsClean} = 1  where {ManagerTableSchema.DatabaseName} = '{database.Name}'");
                        Contract.Assert(releasedDBs == 1);
                        Console.WriteLine($"Released:{database.Name}");
                    }
                );
        }

        private IEnumerable<Database> GetDatabases()
        {
            return _managerConnection.UseCommand(
                command =>
                {
                    var names = new List<Database>();
                    command.CommandText =
                        $"select {ManagerTableSchema.DatabaseName}, {ManagerTableSchema.IsFree}, {ManagerTableSchema.IsClean}, {ManagerTableSchema.ReservationDate} from {ManagerTableSchema.TableName} With(TABLOCKX)";
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            names.Add(new Database(name:reader.GetString(0), isFree: reader.GetBoolean(1), isEmpty: reader.GetBoolean(2), reservationDate: reader.GetDateTime(3), connectionString:ConnectionStringForDbNamed(reader.GetString(0))));
                        }
                    }
                    return names;
                });
        }

        private void ReleaseOldLocks()
        {            
            var count = _managerConnection.ExecuteNonQuery(
                $"update {ManagerTableSchema.TableName} With(TABLOCKX) set {ManagerTableSchema.IsFree} = 1 where {ManagerTableSchema.ReservationDate} < dateadd(minute, -10, getdate()) and {ManagerTableSchema.IsFree} = 0");
            if(count > 0)
            {
                Console.WriteLine($"Released {count} garbage reservations.");
            }
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

        ~TestDatabasePool()
        {
            throw new Exception("Finalizer hit!");
            Console.WriteLine("Finalizer hit!");
            InternalDispose();
        }

        private class Database
        {
            public string Name { get; }
            public bool IsEmpty { get; set; }
            public bool IsFree { get; set; }
            public DateTime ReservationDate { get; set; }
            public string ConnectionString { get; }
            public Database(string name, bool isEmpty, bool isFree, DateTime reservationDate,  string connectionString)
            {
                Name = name;
                IsEmpty = isEmpty;
                IsFree = isFree;
                ReservationDate = reservationDate;
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
                Console.WriteLine(dropCommand);
                try
                {
                    _masterConnection.ExecuteNonQuery(dropCommand);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            _managerConnection.ExecuteNonQuery($"delete {ManagerTableSchema.TableName}");
        }
    }
}