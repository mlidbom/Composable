using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Testing.Databases
{
    sealed partial class SqlServerDatabasePool : StrictlyManagedResourceBase<SqlServerDatabasePool>
    {
        readonly string _masterConnectionString;
        readonly SqlServerConnection _masterConnection;

        readonly MachineWideSharedObject<SharedState> _machineWideState;
        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(30.Seconds());

        static readonly string DatabaseRootFolderOverride;
        static readonly HashSet<string> RebootedMasterConnections = new HashSet<string>();

        readonly Guid _poolId = Guid.NewGuid();

        static SqlServerDatabasePool()
        {
            var tempDirectory = Environment.GetEnvironmentVariable("COMPOSABLE_TEMP_DRIVE");
            if(tempDirectory.IsNullOrWhiteSpace())
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

        ILogger _log = Logger.For<SqlServerDatabasePool>();

        public void SetLogLevel(LogLevel logLevel) => _guard.Update(() => _log = _log.WithLogLevel(logLevel));

        internal static readonly string PoolDatabaseNamePrefix = $"{nameof(SqlServerDatabasePool)}_";

        public SqlServerDatabasePool(string masterConnectionString)
        {
            _machineWideState = MachineWideSharedObject<SharedState>.For(masterConnectionString, usePersistentFile: true);
            _masterConnectionString = masterConnectionString;

            OldContract.Assert.That(_masterConnectionString.Contains(InitialCatalogMaster),
                                    $"MasterDB connection string must contain the exact string: '{InitialCatalogMaster}' this is required for technical optimization reasons");
            _masterConnection = new SqlServerConnection(_masterConnectionString);
        }

        bool _disposed;
        const string InitialCatalogMaster = ";Initial Catalog=master;";

        IReadOnlyList<Database> _transientCache = new List<Database>();
        public ISqlConnection ConnectionProviderFor(string reservationName) => _guard.Update(() =>
        {
            OldContract.Assert.That(!_disposed, "!_disposed");

            var reservedDatabase = _transientCache.SingleOrDefault(db => db.IsReserved && db.ReservedByPoolId == _poolId && db.ReservationName == reservationName);
            if(reservedDatabase != null)
            {
                _log.Debug($"Retrieved reserved pool database: {reservedDatabase.Id}");
                return new Connection(reservedDatabase, reservationName, this);
            }

            var startTime = DateTime.Now;
            var timeoutAt = startTime + 15.Seconds();
            while(reservedDatabase == null)
            {
                if(DateTime.Now > timeoutAt)
                {
                    throw new Exception("Timed out waiting for database. Have you missed disposing a database pool? Please check your logs for errors about non-disposed pools.");
                }

                Exception thrownException = null;
                TransactionScopeCe.SuppressAmbient(
                    () => _machineWideState.Update(
                        machineWide =>
                        {
                            try
                            {
                                if(machineWide.IsEmpty)
                                {
                                    RebootPool(machineWide);
                                }

                                if(!machineWide.IsValid())
                                {
                                    _log.Error(null, "Detected corrupt database pool. Rebooting pool");
                                    RebootPool(machineWide);
                                    thrownException = new Exception("Detected corrupt database pool.Rebooting pool");
                                }

                                if(machineWide.TryReserve(out reservedDatabase, reservationName, _poolId))
                                {
                                    ResetDatabase(reservedDatabase);
                                    _log.Info($"Reserved pool database: {reservedDatabase.Id}");
                                    _transientCache = machineWide.DatabasesReservedBy(_poolId);
                                }
                            }
                            catch(Exception exception)
                            {
                                RebootPool(machineWide);
                                thrownException = exception;
                            }
                        }));

                if(thrownException != null)
                {
                    throw new Exception("Something went wrong with the database pool and it was rebooted. You may see other test failures due to this", thrownException);
                }

                if(reservedDatabase == null)
                {
                    Thread.Sleep(10);
                }
            }

            return new Connection(reservedDatabase, reservationName, this);
        });

        void ResetDatabase(Database db)
        {
            TransactionScopeCe.SuppressAmbient(
                () => new SqlServerConnection(db.ConnectionString(this))
                    .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel()));
        }

        internal string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString.Replace(InitialCatalogMaster, $";Initial Catalog={dbName};");

        Database InsertDatabase(SharedState machineWide)
        {
            var database = machineWide.Insert();

            _log.Warning($"Creating database: {database.Id}");
            using(new TransactionScope(TransactionScopeOption.Suppress))
            {
                CreateDatabase(database.Name());
            }
            return database;
        }

        protected override void InternalDispose()
        {
            if(_disposed) return;
            _disposed = true;
            _machineWideState.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
        }
    }
}
