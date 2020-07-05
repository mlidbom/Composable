using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System;
using Composable.System.Reflection;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Persistence.SqlServer.Testing.Databases
{
    abstract partial class DatabasePool : StrictlyManagedResourceBase<DatabasePool>
    {
        const string InitialCatalogMaster = ";Initial Catalog=master;";

        string? _masterConnectionString;
        static SqlServerConnectionProvider? _masterConnectionProvider;

        MachineWideSharedObject<SharedState>? _machineWideState;

        static string? _databaseRootFolderOverride;
        static readonly HashSet<string> RebootedMasterConnections = new HashSet<string>();

        static TimeSpan _reservationLength;

        public DatabasePool()
        {
            _reservationLength = global::System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 30.Seconds();

            if(ComposableTempFolder.IsOverridden)
            {
                _databaseRootFolderOverride = ComposableTempFolder.EnsureFolderExists("DatabasePoolData");
            }

            var composableDatabasePoolMasterConnectionstringName = ConnectionStringConfigurationParameterName;
            var masterConnectionString = Environment.GetEnvironmentVariable(composableDatabasePoolMasterConnectionstringName);
            _masterConnectionString = masterConnectionString ?? $"Data Source=localhost{InitialCatalogMaster}Integrated Security=True;";

            _masterConnectionString = _masterConnectionString.Replace("\\", "_");

            _machineWideState = MachineWideSharedObject<SharedState>.For(GetType().GetFullNameCompilable().Replace(".", "_"), usePersistentFile: true);

            _masterConnectionProvider = new SqlServerConnectionProvider(_masterConnectionString);

            Contract.Assert.That(_masterConnectionString.Contains(InitialCatalogMaster),
                                 $"MasterDB connection string must contain the exact string: '{InitialCatalogMaster}' this is required for technical optimization reasons");

        }

        protected abstract string ConnectionStringConfigurationParameterName { get; }

        internal static readonly string PoolDatabaseNamePrefix = $"Composable_{nameof(DatabasePool)}_";

        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(30.Seconds());
        readonly Guid _poolId = Guid.NewGuid();
        IReadOnlyList<Database> _transientCache = new List<Database>();

        ILogger _log = Logger.For<DatabasePool>();
        bool _disposed;

        public void SetLogLevel(LogLevel logLevel) => _guard.Update(() => _log = _log.WithLogLevel(logLevel));

        public string ConnectionStringFor(string reservationName) => _guard.Update(() =>
        {
            Contract.Assert.That(!_disposed, "!_disposed");

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
                            if(machineWide.IsEmpty) throw new Exception("MachineWide was empty.");
                            if(!machineWide.IsValid) throw new Exception("Detected corrupt database pool.");

                            if(machineWide.TryReserve(out reservedDatabase, reservationName, _poolId, _reservationLength))
                            {
                                _log.Info($"Reserved pool database: {reservedDatabase.Id}");
                                _transientCache = machineWide.DatabasesReservedBy(_poolId);
                            }
                        }
                        catch(Exception exception)
                        {
                            thrownException = Catch(() => throw new Exception("Encountered exception reserving database. Rebooting pool", exception));
                            RebootPool(machineWide);
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

        static Exception Catch(Action generateException)
        {
            try
            {
                generateException();
            }
            catch(Exception e)
            {
                return e;
            }
            throw new Exception("Exception should have been thrown by now.");
        }

        void ResetDatabase(Database db)
        {
            TransactionScopeCe.SuppressAmbient(
                () => new SqlServerConnectionProvider(db.ConnectionString(this))
                    .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel()));
        }

        internal string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString!.Replace(InitialCatalogMaster, $";Initial Catalog={dbName};");

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
            _machineWideState!.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
            _machineWideState.Dispose();
        }
    }
}
