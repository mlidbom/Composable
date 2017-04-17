using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Transactions;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool : StrictlyManagedResourceBase<SqlServerDatabasePool>
    {
        readonly string _masterConnectionString;
        readonly SqlServerConnectionUtilities _masterConnection;
        readonly SqlServerConnectionUtilities _managerConnection;
        bool _initialized;

        static readonly string DatabaseRootFolderOverride;

        static SqlServerDatabasePool()
        {
            var tempDirectory = Environment.GetEnvironmentVariable("COMPOSABLE_TEMP_DRIVE");
            if (tempDirectory.IsNullOrWhiteSpace())
                return;

            if(!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            DatabaseRootFolderOverride = Path.Combine(tempDirectory, "DatabasePoolData");
            if(!Directory.Exists(DatabaseRootFolderOverride))
            {
                Directory.CreateDirectory(DatabaseRootFolderOverride);
            }
        }

        static readonly ILogger Log = Logger.For<SqlServerDatabasePool>();

        static readonly string ManagerDbName = $"{nameof(SqlServerDatabasePool)}";

        public SqlServerDatabasePool(string masterConnectionString)
        {
            _masterConnectionString = masterConnectionString;
            _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);

            var managerConnectionString = masterConnectionString.Replace(";Initial Catalog=master;", $";Initial Catalog={ManagerDbName};");
            if(managerConnectionString == _masterConnectionString)
            {
                throw new ArgumentException("masterConnectionString must contain the exact string: ';Initial Catalog=master;' in order for the manager connection string to be constructed.");
            }
            _managerConnection = new SqlServerConnectionUtilities(managerConnectionString);
        }

        readonly Dictionary<string, Database> _reservedDatabases = new Dictionary<string, Database>();
        bool _disposed;

        public string ConnectionStringFor(string connectionStringName)
        {
            Contract.Assert.That(!_disposed, "!_disposed");
            EnsureInitialized();

            Database database;
            if(_reservedDatabases.TryGetValue(connectionStringName, out database))
                return database.ConnectionString;

            RunInIsolatedTransaction(action: () =>
                                             {
                                                 if (TryReserveDatabase(out database))
                                                 {
                                                     _reservedDatabases.Add(connectionStringName, database);
                                                 } else
                                                 {
                                                     ReleaseOldLocks();
                                                     if(TryReserveDatabase(out database))
                                                     {
                                                         _reservedDatabases.Add(connectionStringName, database);
                                                     } else
                                                     {
                                                         database = InsertDatabase();
                                                         _reservedDatabases.Add(connectionStringName, database);
                                                     }
                                                 }
                                             });

            return database.ConnectionString;
        }

        void EnsureInitialized()
        {
            if(!_initialized)
            {
                SeparatelyForceInitializationOfManagerConnectionPoolToProvideSanityWhenPerformanceProfiling();
                EnsureManagerDbExistsAndIsAvailable();
                _initialized = true;
            }
        }

        void SeparatelyForceInitializationOfManagerConnectionPoolToProvideSanityWhenPerformanceProfiling()
        {
            try { TransactionScopeCe.SupressAmbient(() => _managerConnection.UseConnection(_ => {})); }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }

        string ConnectionStringForDbNamed(string dbName)
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString) {InitialCatalog = dbName};
            return sqlConnectionStringBuilder.ConnectionString;
        }

        bool TryReserveDatabase(out Database database)
        {
            var commandText = $@"
declare @reservedId integer
SET @reservedId = (select top 1 {ManagerTableSchema.Id} from {ManagerTableSchema.TableName} {ExclusiveTableLockHint} WHERE {ManagerTableSchema.IsFree} = 1 order by {ManagerTableSchema.ReservationDate} asc)

if ( @reservedId is not null)
	update {ManagerTableSchema.TableName} 
        set {ManagerTableSchema.IsFree} = 0, 
            {ManagerTableSchema.ReservationDate} = getdate(), 
            {ManagerTableSchema.ReservationCallStack} = @{ManagerTableSchema.ReservationCallStack}
    where Id = @reservedId

select @reservedId";

            Database otherDb = null;
            _managerConnection.UseCommand(command =>
                                          {
                                              command.CommandType = CommandType.Text;
                                              command.CommandText = commandText;
                                              command.Parameters.Add(new SqlParameter(ManagerTableSchema.ReservationCallStack, SqlDbType.VarChar, -1) {Value = Environment.StackTrace});

                                              var idObject = command.ExecuteScalar();

                                              if (!(idObject is DBNull))
                                              {
                                                otherDb = new Database(pool: this, id: (int)idObject);
                                              }
                                          });

            database = otherDb;
            return database != null;
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
    }
}
