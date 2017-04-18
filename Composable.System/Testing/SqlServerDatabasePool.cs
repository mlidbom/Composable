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

        static readonly ILogger Log = Logger.For<SqlServerDatabasePool>();

        internal static readonly string PoolDatabaseNamePrefix = $"{nameof(SqlServerDatabasePool)}_";

        public SqlServerDatabasePool(string masterConnectionString)
        {
            _machineWideState = MachineWideSharedObject<SharedState>.For(masterConnectionString, usePersistentFile: true);
            _masterConnectionString = masterConnectionString;

            Contract.Assert.That(_masterConnectionString.Contains(_initialCatalogMaster),
                                 $"MasterDB connection string must contain the exact string: '{_initialCatalogMaster}' this is required for technical optimization reasons");
            _masterConnection = new SqlServerConnectionProvider(_masterConnectionString);
        }

        readonly Dictionary<string, Database> _reservedDatabases = new Dictionary<string, Database>();
        bool _disposed;
        string _initialCatalogMaster = ";Initial Catalog=master;";

        public ISqlConnectionProvider ConnectionProviderFor(string reservationName)
        {
            Contract.Assert.That(!_disposed, "!_disposed");

            Database database;
            if(_reservedDatabases.TryGetValue(reservationName, out database))
            {
                Log.Info($"Retrieved reserved pool database: {database.Id}");
                return new ConnectionProvider(database, this);
            }

            void SaveReservedDatabase()
            {
                Contract.Assert.That(database.IsReserved, "database.IsReserved");
                Contract.Assert.That(database.ReservationName == reservationName, "database.ReservationName == reservationName");
                _reservedDatabases.Add(reservationName, database);
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

                            if(!machineWide.TryReserve(out database, reservationName))
                            {
                                ReleaseOldLocks(machineWide);
                                if (!machineWide.TryReserve(out database, reservationName))
                                {
                                    database = InsertDatabase(machineWide);
                                    database.Reserve(reservationName);
                                }
                            }
                            SaveReservedDatabase();
                        }));

            Log.Info($"Reserved pool database: {database.Id}");
            return new ConnectionProvider(database, this);
        }

        internal string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString.Replace(_initialCatalogMaster,$";Initial Catalog={dbName};");

        Database InsertDatabase(SharedState machineWide)
        {
            Database database = machineWide.Insert();

            Log.Warning($"Creating database: {database.Name()}");
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                CreateDatabase(database.Name());
            }
            return database;
        }

        void ReleaseOldLocks(SharedState machineWide)
        {
            var databases = machineWide.DbsWithOldLocks();

            void CleanAndReleaseDatabase(Database database)
            {
                Log.Info($"Cleaning and releasing from old locks: {database.Id}");
                new SqlServerConnectionProvider(database.ConnectionString(this)).UseConnection(action: connection => connection.DropAllObjects());

                machineWide.Release(database.Id);
            }

            databases.ForEach(action: db => _reservedDatabases.Remove(db.Name()));
            databases.ForEach(CleanAndReleaseDatabase);
        }

        void CleanAndRelease(IReadOnlyList<Database> databases)
        {
            void CleanAndReleaseDatabase(Database database)
            {
                Log.Info($"Cleaning and releasing: {database.Id}");
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
