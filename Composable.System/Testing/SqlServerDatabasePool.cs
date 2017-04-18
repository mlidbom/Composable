using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Transactions;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool : StrictlyManagedResourceBase<SqlServerDatabasePool>
    {
        readonly string _masterConnectionString;
        readonly SqlServerConnectionProvider _masterConnection;

        readonly MachineWideSharedObject<SharedState> _machineWideState;

        static readonly string DatabaseRootFolderOverride;
        static readonly HashSet<string> RebootedMasterConnections = new HashSet<string>();

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

        readonly ILogger _log = Logger.For<SqlServerDatabasePool>();

        public void SetLogLevel(LogLevel logLevel) => _log.SetLogLevel(logLevel);

        internal static readonly string PoolDatabaseNamePrefix = $"{nameof(SqlServerDatabasePool)}_";

        public SqlServerDatabasePool(string masterConnectionString)
        {
            _machineWideState = MachineWideSharedObject<SharedState>.For(masterConnectionString, usePersistentFile: true);
            _masterConnectionString = masterConnectionString;

            Contract.Assert.That(_masterConnectionString.Contains(InitialCatalogMaster),
                                 $"MasterDB connection string must contain the exact string: '{InitialCatalogMaster}' this is required for technical optimization reasons");
            _masterConnection = new SqlServerConnectionProvider(_masterConnectionString);
        }

        readonly Dictionary<string, Database> _reservedDatabases = new Dictionary<string, Database>();
        bool _disposed;
        const string InitialCatalogMaster = ";Initial Catalog=master;";

        public ISqlConnectionProvider ConnectionProviderFor(string reservationName)
        {
            Contract.Assert.That(!_disposed, "!_disposed");

            Database database;
            if(_reservedDatabases.TryGetValue(reservationName, out database))
            {
                _log.Debug($"Retrieving reserved pool database: {database.Id}");
                if(!database.IsReserved)
                {
                    throw new Exception("Db has somehow been released. The pool is probably corrupt and beeing rebooted. Try rerunning your tests");
                }
                return new ConnectionProvider(database, this);
            }


            TransactionScopeCe.SupressAmbient(
                () =>
                    _machineWideState.Update(
                        machineWide =>
                        {
                            if(!machineWide.IsValid())
                            {
                                RebootPool(machineWide);
                            }

                            ReleaseOldLocks(machineWide);
                            if(!machineWide.TryReserve(out database, reservationName))
                            {
                                database = InsertDatabase(machineWide);
                                database.Reserve(reservationName);
                            }

                            Contract.Assert.That(database.IsReserved, "database.IsReserved");
                            Contract.Assert.That(database.ReservationName == reservationName, "database.ReservationName == reservationName");
                            _reservedDatabases.Add(reservationName, database);

                        }));

            _log.Info($"Reserved pool database: {database.Id}");
            return new ConnectionProvider(database, this);
        }

        internal string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString.Replace(InitialCatalogMaster,$";Initial Catalog={dbName};");

        Database InsertDatabase(SharedState machineWide)
        {
            Database database = machineWide.Insert();

            _log.Warning($"Creating database: {database.Id}");
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                CreateDatabase(database.Name());
            }
            return database;
        }

        void ReleaseOldLocks(SharedState machineWide)
        {
            var toGarbageCollect = machineWide.DbsWithOldLocks();

            void CleanAndReleaseDatabase(Database database)
            {
                _log.Info($"Cleaning and releasing from old locks: {database.Id}");
                new SqlServerConnectionProvider(database.ConnectionString(this)).UseConnection(action: connection => connection.DropAllObjects());

                machineWide.Release(database.Id);
            }

            toGarbageCollect.ForEach(action: db =>
                                      {
                                          var dbWasRemovedFromReserved = _reservedDatabases.Remove(db.ReservationName);
                                          Contract.Assert.That(db.ReservationName != string.Empty, "db.ReservationDate != string.Empty");
                                          Contract.Assert.That(!dbWasRemovedFromReserved, "!dbWasRemovedFromReserved");
                                      });
            toGarbageCollect.ForEach(CleanAndReleaseDatabase);
        }

        void CleanAndRelease(IReadOnlyList<Database> databases)
        {
            void CleanAndReleaseDatabase(Database database)
            {
                _log.Debug($"Cleaning and releasing: {database.Id}");
                TransactionScopeCe.SupressAmbient(
                    () => new SqlServerConnectionProvider(database.ConnectionString(this)).UseConnection(action: connection => connection.DropAllObjects()));

                _machineWideState.Update(machineWide => machineWide.Release(database.Id));
            }

            databases.ForEach(action: db =>
                                      {
                                          var dbWasRemovedFromReserved = _reservedDatabases.Remove(db.ReservationName);
                                          Contract.Assert.That(dbWasRemovedFromReserved, "dbWasRemovedFromReserved");
                                      });
            Task.Run(()=>  databases.ForEach(CleanAndReleaseDatabase));
        }

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
