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

        readonly Guid _poolId = Guid.NewGuid();

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

        ILogger _log = Logger.For<SqlServerDatabasePool>();

        public void SetLogLevel(LogLevel logLevel) => _log = _log.WithLogLevel(logLevel);

        internal static readonly string PoolDatabaseNamePrefix = $"{nameof(SqlServerDatabasePool)}_";

        public SqlServerDatabasePool(string masterConnectionString)
        {
            _machineWideState = MachineWideSharedObject<SharedState>.For(masterConnectionString, usePersistentFile: true);
            _masterConnectionString = masterConnectionString;

            Contract.Assert.That(_masterConnectionString.Contains(InitialCatalogMaster),
                                 $"MasterDB connection string must contain the exact string: '{InitialCatalogMaster}' this is required for technical optimization reasons");
            _masterConnection = new SqlServerConnectionProvider(_masterConnectionString);
        }

        bool _disposed;
        const string InitialCatalogMaster = ";Initial Catalog=master;";

        IReadOnlyList<Database> _transientCache = new List<Database>();
        public ISqlConnectionProvider ConnectionProviderFor(string reservationName)
        {
            Contract.Assert.That(!_disposed, "!_disposed");

            Database database = _transientCache.SingleOrDefault(db => db.IsReserved && db.ReservedByPoolId == _poolId && db.ReservationName == reservationName);
            if(database != null)
            {
                _log.Debug($"Retrieved reserved pool database: {database.Id}");
            } else
            {
                SharedState snapshot = null;
                TransactionScopeCe.SupressAmbient(
                    () =>
                        _machineWideState.Update(
                            machineWide =>
                            {
                                if(!machineWide.IsValid())
                                {
                                    _log.Error(null, "Detected corruct database pool. Rebooting pool");
                                    RebootPool(machineWide);
                                }


                                if (machineWide.TryReserve(out database, reservationName, _poolId))
                                {
                                    _log.Info($"Reserved pool database: {database.Id}");
                                } else
                                {
                                    database = InsertDatabase(machineWide);
                                    database.Reserve(reservationName, _poolId);
                                }

                                Contract.Assert.That(database.IsClean, "database.IsClean");

                                _transientCache = machineWide.DatabasesReservedBy(_poolId);
                                snapshot = machineWide;
                            }));

                if (snapshot.NeedsGarbageCollection())
                {
                    ScheduleGarbageCollectionOnBackgroundThread();
                }

            }

            return new ConnectionProvider(database, reservationName, this);
        }


        void ScheduleGarbageCollectionOnBackgroundThread()
        {
            Task.Run(() =>
                     {
                         IReadOnlyList<Database> toCleanAndRelease = null;
                         _machineWideState.Update(machineWide =>
                                                  {
                                                      if(machineWide.NeedsGarbageCollection())
                                                      {
                                                          toCleanAndRelease = machineWide.ReserveDatabasesForGarbageCollection();
                                                      }
                                                  });

                         toCleanAndRelease.ForEach(db => Task.Run(() =>
                                                           {
                                                               _log.Debug($"Cleaning: {db.Id}");
                                                               try
                                                               {
                                                                   TransactionScopeCe.SupressAmbient(
                                                                       () => new SqlServerConnectionProvider(db.ConnectionString(this))
                                                                           .UseConnection(action: connection => connection.DropAllObjects()));
                                                                   _machineWideState.Update(machineWide => machineWide.Release(db.Id)
                                                                                                                      .Clean());
                                                               }
                                                               catch(Exception exception)
                                                               {
                                                                   ScheduleForRebooting();
                                                                   throw new Exception("Garbage collection failed and pool has been scheduled for Reboot", exception);
                                                               }
                                                           }));
                     });
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

        void ReleaseReservedOnBackgroundThread()
        {
            Task.Run(() => _machineWideState.Update(machineWide => machineWide.DatabasesReservedBy(_poolId)
                                                                              .ForEach(db => db.Release())));
        }

        protected override void InternalDispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ReleaseReservedOnBackgroundThread();
            }
        }
    }
}
