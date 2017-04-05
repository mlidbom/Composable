using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
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

        static readonly string DatabaseRootFolder;

        static SqlServerDatabasePool()
        {
            var tempDirectory = Environment.GetEnvironmentVariable("COMPOSABLE_TEMP_DRIVE");
            tempDirectory = tempDirectory ?? Path.Combine(Path.GetTempPath(), "COMPOSABLE_TMP");
            if(!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            DatabaseRootFolder = Path.Combine(tempDirectory, "DatabasePoolData");
            if(!Directory.Exists(DatabaseRootFolder))
            {
                Directory.CreateDirectory(DatabaseRootFolder);
            }
        }

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

        public string ConnectionStringFor(string requestedDbName)
        {
            if(_disposed)
                throw new InvalidOperationException(message: "Attempt to use disposed object");

            Database database;
            if(_reservedDatabases.TryGetValue(requestedDbName, out database))
                return database.ConnectionString;

            RunInIsolatedTransaction(action: () =>
                                             {
                                                 if (TryReserveDatabase(out database))
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
                                                         database = InsertDatabase();
                                                         _reservedDatabases.Add(requestedDbName, database);
                                                     }
                                                 }
                                             });

            return database.ConnectionString;
        }

        string ConnectionStringForDbNamed(string dbName)
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString) {InitialCatalog = dbName};
            return sqlConnectionStringBuilder.ConnectionString;
        }

        bool TryReserveDatabase(out Database database)
        {
            var command = $@"
declare @reservedId integer
SET @reservedId = (select top 1 {ManagerTableSchema.Id} from {ManagerTableSchema.TableName} {ExclusiveTableLockHint} WHERE {ManagerTableSchema.IsFree} = 1 order by {ManagerTableSchema.ReservationDate} asc)

if ( @reservedId is not null)
	update {ManagerTableSchema.TableName} 
        set {ManagerTableSchema.IsFree} = 0, 
            {ManagerTableSchema.ReservationDate} = getdate(), 
            {ManagerTableSchema.ReservationCallStack} = '{Environment.StackTrace}'
    where Id = @reservedId

select @reservedId";

            var idObject = _managerConnection.ExecuteScalar(command);
            if(idObject is DBNull)
            {
                database = null;
                return false;
            }

            database = new Database(pool: this, id: (int)idObject);
            return true;
        }

        Database InsertDatabase()
        {
            var value = _managerConnection.ExecuteScalar(
                $@"
                set nocount on
                insert {ManagerTableSchema.TableName} ({ManagerTableSchema.IsFree}, {ManagerTableSchema.ReservationDate},  {ManagerTableSchema.ReservationCallStack}) 
                                                   values(                0      ,                     getdate()       ,                     '{Environment.StackTrace}')
                select @@IDENTITY");
            var id = (int)(decimal)value;
            var database = new Database(pool: this, id: id);
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                CreateDatabase(database.Name);
            }
            return database;
        }


        void ReleaseOldLocks()
        {
            var selectDbsWithOldLocks = $"select {ManagerTableSchema.Id} from {ManagerTableSchema.TableName} {ExclusiveTableLockHint} where {ManagerTableSchema.ReservationDate} < dateadd(minute, -10, getdate()) and {ManagerTableSchema.IsFree} = 0";
            var oldLockedDatabases = new List<Database>();
            _managerConnection.UseCommand(command =>
                                          {
                                              command.CommandText = selectDbsWithOldLocks;
                                              command.CommandType = CommandType.Text;
                                              using(var reader = command.ExecuteReader())
                                              {
                                                  while(reader.Read())
                                                  {
                                                      oldLockedDatabases.Add(new Database(pool: this, id: reader.GetInt32(0)));
                                                  }
                                              }
                                          });
            CleanAndRelease(oldLockedDatabases);
        }

        void CleanAndRelease(IReadOnlyList<Database> databases)
        {
            void CleanAndReleaseDatabase(Database database)
            {
                new SqlServerConnectionUtilities(ConnectionStringForDbNamed(database.Name)).UseConnection(action: connection => connection.DropAllObjects());

                _managerConnection.ExecuteNonQuery($@"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 1  where {ManagerTableSchema.Id} = {database.Id}");
            }

            databases.ForEach(action: db => _reservedDatabases.Remove(db.Name));

            Task.Run(() => databases.ForEach(CleanAndReleaseDatabase));
        }

        static readonly string ExclusiveTableLockHint = "With(TABLOCKX)";

        protected override void InternalDispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                CleanAndRelease(_reservedDatabases.Values.ToList());
            }
        }

        ~SqlServerDatabasePool()
        {
            InternalDispose();
        }
    }
}
