using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Testing.Databases
{
    abstract partial class DatabasePool : StrictlyManagedResourceBase<DatabasePool>
    {
        readonly MachineWideSharedObject<SharedState> _machineWideState;
        protected static string? DatabaseRootFolderOverride;
        static TimeSpan _reservationLength;
        static readonly int NumberOfDatabases = 30;

        protected DatabasePool()
        {
            _reservationLength = global::System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 30.Seconds();

            if(ComposableTempFolder.IsOverridden)
            {
                DatabaseRootFolderOverride = ComposableTempFolder.EnsureFolderExists("DatabasePoolData");
            }

            _machineWideState = MachineWideSharedObject<SharedState>.For(GetType().GetFullNameCompilable().Replace(".", "_"), usePersistentFile: true);

        }

        internal static readonly string PoolDatabaseNamePrefix = $"Composable_{nameof(DatabasePool)}_";

        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(30.Seconds());
        readonly Guid _poolId = Guid.NewGuid();
        IReadOnlyList<Database> _transientCache = new List<Database>();

        static ILogger Log = Logger.For<DatabasePool>();
        bool _disposed;
        static readonly string RebootedDatabaseExceptionMessage = "Something went wrong with the database pool and it was rebooted. You may see other test failures due to this. If this is the first time you use the pool everything is fine. If this error pops up at other times something is amiss.";

        public void SetLogLevel(LogLevel logLevel) => _guard.Update(() => Log = Log.WithLogLevel(logLevel));

        public string ConnectionStringFor(string reservationName) => _guard.Update(() =>
        {
            Contract.Assert.That(!_disposed, "!_disposed");

            var reservedDatabase = _transientCache.SingleOrDefault(db => db.ReservationName == reservationName);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(reservedDatabase != null)
            {
                Log.Debug($"Retrieved reserved pool database: {reservedDatabase.Id}");
                return this.ConnectionStringFor(reservedDatabase);
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
                _machineWideState.Update(
                    machineWide =>
                    {
                        try
                        {
                            if(machineWide.IsEmpty) throw new Exception("MachineWide was empty.");
                            if(!machineWide.IsValid) throw new Exception("Detected corrupt database pool.");

                            if(machineWide.TryReserve(reservationName, _poolId, _reservationLength, out reservedDatabase))
                            {
                                Log.Info($"Reserved pool database: {reservedDatabase.Id}");
                                _transientCache = machineWide.DatabasesReservedBy(_poolId);
                                //My tests so far show no performance increase from enabling this.
                                //CleanDataBasesInBackgroundTasks(machineWide);
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
                    throw new Exception(RebootedDatabaseExceptionMessage, thrownException);
                }

                if(reservedDatabase == null)
                {
                    Thread.Sleep(10);
                }
            }

            if(!reservedDatabase.IsClean)
            {
                try
                {
                    ResetDatabase(reservedDatabase);
                }
                catch(Exception exception)
                {
                    RebootPool();
                    throw new Exception(RebootedDatabaseExceptionMessage, exception);
                }
            }

            return this.ConnectionStringFor(reservedDatabase);
        });

        void CleanDataBasesInBackgroundTasks(SharedState machineWide)
        {
            machineWide.ReserveDatabasesForCleaning(_poolId)
                       .ForEach(reserved =>
                        {
                            Task.Run(() =>
                            {
                                //We are being a bit tricky here. The databases are reserved by this instance so that when it is disposed so are their reservations.
                                //That way we don't leave our reservations around when we don't have time to clean them before the test runner terminates.
                                //But that means we must be careful not to mess with databases that have been released when this pool was disposed.
                                lock(_disposeLock)
                                {
                                    if(!_disposed)
                                    {
                                        ResetDatabase(reserved);
                                        _machineWideState.Update(innerMachineWide => innerMachineWide.ReleaseClean(reserved.ReservationName));
                                    }
                                }
                            });
                        });
        }

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

        protected abstract void ResetDatabase(Database db);

        protected abstract string ConnectionStringFor(Database db);

        Database InsertDatabase(SharedState machineWide)
        {
            var database = machineWide.Insert();

            using(new TransactionScope(TransactionScopeOption.Suppress))
            {
                EnsureDatabaseExistsAndIsEmpty(database);
            }
            return database;
        }

        readonly object _disposeLock = new object();
        protected override void InternalDispose()
        {
            lock(_disposeLock)
            {
                if(_disposed) return;
                _disposed = true;
            }
            _machineWideState.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
            _machineWideState.Dispose();
        }

        void RebootPool() => _machineWideState.Update(RebootPool);

        void RebootPool(SharedState machineWide) => TransactionScopeCe.SuppressAmbient(() =>
        {
            Log.Warning("Rebooting database pool");

            machineWide.Reset();
            _transientCache = new List<Database>();

            1.Through(NumberOfDatabases)
             .Select(index => new Database(index))
             .ForEach(db => InsertDatabase(machineWide));
        });

        protected abstract void EnsureDatabaseExistsAndIsEmpty(Database db);
    }
}
