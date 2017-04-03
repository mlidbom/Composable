using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool : StrictlyManagedResourceBase<SqlServerDatabasePool>
    {
        readonly string _masterConnectionString;
        readonly SqlServerConnectionUtilities _masterConnection;
        readonly SqlServerConnectionUtilities _managerConnection;

        static readonly ILogger Log = Logger.For<SqlServerDatabasePool>();

        static readonly string ManagerDbName = $"{nameof(SqlServerDatabasePool)}";

        public SqlServerDatabasePool(string masterConnectionString)
        {
            _masterConnectionString = masterConnectionString;
            _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);


            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString) {InitialCatalog = ManagerDbName};
            _managerConnection = new SqlServerConnectionUtilities(sqlConnectionStringBuilder.ConnectionString);

            EnsureManagerDbExists();
        }

        readonly Dictionary<string, Database> _reservedDatabases = new Dictionary<string, Database>();
        bool _disposed;

        static void RunInIsolatedTransaction(Action action)
        {
            using(var transaction = new TransactionScope(TransactionScopeOption.Required,
                                                         new TransactionOptions
                                                         {
                                                             IsolationLevel = IsolationLevel.Serializable
                                                         }))
            {
                action();
                transaction.Complete();
            }
        }

        public string ConnectionStringFor(string requestedDbName)
        {
            if(_disposed)
            {
                throw new InvalidOperationException("Attempt to use disposed object");
            }

            Database database;
            if(_reservedDatabases.TryGetValue(requestedDbName, out database))
            {
                return database.ConnectionString;
            }

            bool newDatabase = false;
            RunInIsolatedTransaction(() =>
                                     {
                                         if(TryReserveDatabase(out database))
                                         {
                                             _reservedDatabases.Add(requestedDbName, database);
                                         } else
                                         {
                                             ReleaseOldLocks();
                                             if(TryReserveDatabase(out database))
                                             {
                                                 _reservedDatabases.Add(requestedDbName, database);
                                             } else
                                             {
                                                 newDatabase = true;
                                                 // ReSharper disable once AssignNullToNotNullAttribute
                                                 var newDatabaseName = $"{ManagerDbName}_{Guid.NewGuid()}.mdf";
                                                 database = new Database(
                                                     name: newDatabaseName,
                                                     isFree: false,
                                                     connectionString: ConnectionStringForDbNamed(newDatabaseName));

                                                 using(new TransactionScope(TransactionScopeOption.Suppress))
                                                 {
                                                     CreateDatabase(database.Name);
                                                 }

                                                 InsertDatabase(database.Name);

                                                 _reservedDatabases.Add(requestedDbName, database);
                                             }
                                         }
                                     });

            if(!newDatabase)
            {
                CleanDatabase(database);
            }
            return database.ConnectionString;
        }        

        string ConnectionStringForDbNamed(string dbName)
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString) {InitialCatalog = dbName};
            return sqlConnectionStringBuilder.ConnectionString;
        }

        bool TryReserveDatabase(out Database database)
        {
            database = null;
            var databases = GetDatabases();
            var freeDbs = databases.Where(db => db.IsFree).ToList();
            if(freeDbs.Any())
            {
                database = freeDbs.First();
                ReserveDatabase(database.Name);
                return true;
            }
            return false;
        }

        void ReserveDatabase(string dbName)
        {
            _managerConnection.ExecuteNonQuery(
                $"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 0, {ManagerTableSchema.ReservationDate} = getdate(), {ManagerTableSchema.ReservationCallStack} = '{Environment.StackTrace}' where {ManagerTableSchema.DatabaseName} = '{dbName}'");
            //SafeConsole.WriteLine($"Reserved:{dbName}");
        }

        void CleanDatabase(Database db)
        {
            new SqlServerConnectionUtilities(ConnectionStringForDbNamed(db.Name))
                .UseConnection(connection => connection.DropAllObjects());
        }

        void InsertDatabase(string dbName)
        {
            _managerConnection.ExecuteNonQuery(
                $@"insert {ManagerTableSchema.TableName} ({ManagerTableSchema.DatabaseName}, {ManagerTableSchema.IsFree}, {ManagerTableSchema.ReservationDate},  {ManagerTableSchema.ReservationCallStack}) 
                                                           values('{dbName}'               ,                     0      ,                     getdate()       ,                     '{Environment.StackTrace}')");
        }

        void ReleaseDatabases(IReadOnlyList<Database> database)
        {
            database.ForEach(db => _reservedDatabases.Remove(db.Name));

            var nameList = database.Select(db => "'" + db.Name + "'").Join(",");

            Task.Run(
                () => RunInIsolatedTransaction(
                    () => _managerConnection.ExecuteNonQuery(
                        $"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 1  where {ManagerTableSchema.DatabaseName} in ({nameList})")));
        }

        static readonly string LockingHint = "With(TABLOCKX)";

        IEnumerable<Database> GetDatabases()
        {
            return _managerConnection.UseCommand(
                command =>
                {
                    var names = new List<Database>();
                    command.CommandText =
                        $"select {ManagerTableSchema.DatabaseName}, {ManagerTableSchema.IsFree}, {ManagerTableSchema.ReservationDate} from {ManagerTableSchema.TableName} {LockingHint}";
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            names.Add(
                                new Database(
                                    name: reader.GetString(0),
                                    isFree: reader.GetBoolean(1),
                                    connectionString: ConnectionStringForDbNamed(reader.GetString(0))));
                        }
                    }
                    return names;
                });
        }

        void ReleaseOldLocks()
        {
                    RunInIsolatedTransaction(() =>
                                             {
                                                 var count = _managerConnection.ExecuteNonQuery(
                                                     $"update {ManagerTableSchema.TableName} {LockingHint} set {ManagerTableSchema.IsFree} = 1 where {ManagerTableSchema.ReservationDate} < dateadd(minute, -10, getdate()) and {ManagerTableSchema.IsFree} = 0");
                                                 if(count > 0)
                                                 {
                                                     //SafeConsole.WriteLine($"Released {count} garbage reservations.");
                                                 }
                                             });
        }

        protected override void InternalDispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ReleaseDatabases(_reservedDatabases.Values.ToList());
            }
        }

        ~SqlServerDatabasePool()
        {
            InternalDispose();
        }

        class Database
        {
            internal string Name { get; }
            internal bool IsFree { get; }
            internal string ConnectionString { get; }
            internal Database(string name,  bool isFree, string connectionString)
            {
                Name = name;
                IsFree = isFree;
                ConnectionString = connectionString;
            }
        }

        // ReSharper disable once UnusedMember.Global
        internal void RemoveAllDatabases()
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
                //SafeConsole.WriteLine(dropCommand);
                try
                {
                    _masterConnection.ExecuteNonQuery(dropCommand);
                }
                catch(Exception exception)
                {
                    Log.Error(exception);
                }
            }

            _managerConnection.ExecuteNonQuery($"delete {ManagerTableSchema.TableName}");
        }
    }
}
