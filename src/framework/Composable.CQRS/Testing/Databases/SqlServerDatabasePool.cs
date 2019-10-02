using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Contracts;
using Composable.GenericAbstractions;
using Composable.Logging;
using Composable.Persistence;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;
using ISqlConnectionProvider = Composable.System.Data.SqlClient.ISqlConnectionProvider;

namespace Composable.Testing.Databases
{
    sealed partial class SqlServerDatabasePool : StrictlyManagedResourceBase<SqlServerDatabasePool>
    {
        readonly IConfigurationParameterProvider _configurationParameterProvider;
        const string InitialCatalogMaster = ";Initial Catalog=master;";

        string _masterConnectionString;
        static SqlServerConnectionProvider _masterConnectionProvider;

        MachineWideSharedObject<SharedState> _machineWideState;

        static string _databaseRootFolderOverride;
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

                var composableDatabasePoolMasterConnectionstringName = "COMPOSABLE_DATABASE_POOL_MASTER_CONNECTIONSTRING";
                var masterConnectionString = Environment.GetEnvironmentVariable(composableDatabasePoolMasterConnectionstringName);
                _masterConnectionString = masterConnectionString ?? new ConfigurationSqlConnectionProviderSource(_configurationParameterProvider).GetConnectionProvider(composableDatabasePoolMasterConnectionstringName).ConnectionString;

                _masterConnectionString = _masterConnectionString.Replace("\\", "_");

                _machineWideState = MachineWideSharedObject<SharedState>.For(_masterConnectionString, usePersistentFile: true);

                _masterConnectionProvider = new SqlServerConnectionProvider(_masterConnectionString);

                Contract.Assert.That(_masterConnectionString.Contains(InitialCatalogMaster),
                                     $"MasterDB connection string must contain the exact string: '{InitialCatalogMaster}' this is required for technical optimization reasons");

                _initialized = true;
            }
        }

        internal static readonly string PoolDatabaseNamePrefix = $"Composable_{nameof(SqlServerDatabasePool)}_";

        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(30.Seconds());
        readonly Guid _poolId = Guid.NewGuid();
        IReadOnlyList<Database> _transientCache = new List<Database>();

        ILogger _log = Logger.For<SqlServerDatabasePool>();
        bool _disposed;

        public SqlServerDatabasePool(IConfigurationParameterProvider configurationParameterProvider) => _configurationParameterProvider = configurationParameterProvider;

        public void SetLogLevel(LogLevel logLevel) => _guard.Update(() => _log = _log.WithLogLevel(logLevel));

        public ISqlConnectionProvider ConnectionProviderFor(string reservationName) => new LazySqlServerConnectionProvider(() => ConnectionStringFor(reservationName));

        string ConnectionStringFor(string reservationName) => _guard.Update(() =>
        {
            Contract.Assert.That(!_disposed, "!_disposed");
            EnsureInitialized();

            var reservedDatabase = _transientCache.SingleOrDefault(db => db.IsReserved && db.ReservedByPoolId == _poolId && db.ReservationName == reservationName);
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

                Exception thrownException = null;
                _machineWideState.Update(
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
                                _log.Error(null, "Detected corrupt database pool. Rebooting pool");
                                RebootPool(machineWide);
                                thrownException = new Exception("Detected corrupt database pool.Rebooting pool");
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
                () => new SqlServerConnectionProvider(db.ConnectionString(this))
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
            if(_disposed || !_initialized) return;
            _disposed = true;
            _machineWideState.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
            _machineWideState.Dispose();
        }
    }
}
