using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Persistence.MySql.Testing.Databases
{
    sealed partial class MySqlDatabasePool : StrictlyManagedResourceBase<MySqlDatabasePool>
    {
        readonly IConfigurationParameterProvider _configurationParameterProvider;
        const string InitialCatalogMaster = ";Database=mysql;";

        string? _masterConnectionString;
        static MySqlConnectionProvider? _masterConnectionProvider;

        MachineWideSharedObject<SharedState>? _machineWideState;

        static string? _databaseRootFolderOverride;
        static readonly HashSet<string> RebootedMasterConnections = new HashSet<string>();

        static TimeSpan _reservationLength;

        bool _initialized;
        readonly object _lockObject = new object();
        void EnsureInitialized()
        {
            lock(_lockObject)
            {
                if(_initialized) return;

                _reservationLength = global::System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 30.Seconds();

                if(ComposableTempFolder.IsOverridden)
                {
                    _databaseRootFolderOverride = ComposableTempFolder.EnsureFolderExists("DatabasePoolData");
                }

                var composableDatabasePoolMasterConnectionstringName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";
                var masterConnectionString = Environment.GetEnvironmentVariable(composableDatabasePoolMasterConnectionstringName);
                _masterConnectionString = masterConnectionString ?? _configurationParameterProvider.GetString(composableDatabasePoolMasterConnectionstringName);

                _masterConnectionString = _masterConnectionString.Replace("\\", "_");

                _machineWideState = MachineWideSharedObject<SharedState>.For(_masterConnectionString, usePersistentFile: true);

                _masterConnectionProvider = new MySqlConnectionProvider(_masterConnectionString);

                Contract.Assert.That(_masterConnectionString.Contains(InitialCatalogMaster),
                                     $"MasterDB connection string must contain the exact string: '{InitialCatalogMaster}' this is required for technical optimization reasons");

                _initialized = true;
            }
        }

        internal static readonly string PoolDatabaseNamePrefix = $"Composable_{nameof(MySqlDatabasePool)}_";

        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(30.Seconds());
        readonly Guid _poolId = Guid.NewGuid();
        IReadOnlyList<Database> _transientCache = new List<Database>();

        ILogger _log = Logger.For<MySqlDatabasePool>();
        bool _disposed;

        public MySqlDatabasePool(IConfigurationParameterProvider configurationParameterProvider) => _configurationParameterProvider = configurationParameterProvider;

        public void SetLogLevel(LogLevel logLevel) => _guard.Update(() => _log = _log.WithLogLevel(logLevel));

        public string ConnectionStringFor(string reservationName) => _guard.Update(() =>
        {
            Contract.Assert.That(!_disposed, "!_disposed");
            EnsureInitialized();

            var reservedDatabase = _transientCache.SingleOrDefault(db => db.IsReserved && db.ReservedByPoolId == _poolId && db.ReservationName == reservationName);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(reservedDatabase != null)
            {
                _log.Debug($"Retrieved reserved pool database: {reservedDatabase.Id}");
                return reservedDatabase.ConnectionString(this);
            }

            var startTime = DateTime.Now;
            var timeoutAt = startTime + 45.Seconds();
            while(reservedDatabase == null)
            {
                if(DateTime.Now > timeoutAt)
                {
                    throw new Exception("Timed out waiting for database. Have you missed disposing a database pool? Please check your logs for errors about non-disposed pools.");
                }

                Exception? thrownException = null;
                _machineWideState!.Update(
                    machineWide =>
                    {
                        try
                        {
                            if(machineWide.IsEmpty)
                            {
                                RebootPool(machineWide);
                            }

                            if(!machineWide.IsValid)
                            {
                                thrownException = new Exception("Detected corrupt database pool.Rebooting pool");
                                _log.Error(thrownException, "Detected corrupt database pool. Rebooting pool");
                                RebootPool(machineWide);
                            }

                            if(machineWide.TryReserve(out reservedDatabase, reservationName, _poolId, _reservationLength))
                            {
                                _log.Info($"Reserved pool database: {reservedDatabase.Id}");
                                _transientCache = machineWide.DatabasesReservedBy(_poolId);
                            }
                        }
                        catch(Exception exception)
                        {
                            RebootPool(machineWide);
                            thrownException = exception;
                        }
                    });

                if(thrownException != null)
                {
                    throw new Exception("Something went wrong with the database pool and it was rebooted. You may see other test failures due to this", thrownException);
                }

                if(reservedDatabase == null)
                {
                    Thread.Sleep(10);
                }
            }

            try
            {
                ResetDatabase(reservedDatabase);
            }
            catch(Exception exception)
            {
                _log.Error(exception, "Something went wrong with the database pool and it will be rebooted.");
                RebootPool();
                throw new Exception("Something went wrong with the database pool and it was rebooted. You may see other test failures due to this", exception);
            }

            return reservedDatabase.ConnectionString(this);
        });

        void ResetDatabase(Database db)
        {
            TransactionScopeCe.SuppressAmbient(
                () => new MySqlConnectionProvider(db.ConnectionString(this))
                    .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel(db.Name())));
        }

        internal string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString!.Replace(InitialCatalogMaster, $";Database={dbName};");

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
            if(_disposed || !_initialized) return;
            _disposed = true;
            _machineWideState!.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
            _machineWideState.Dispose();
        }
    }
}
