using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Testing.Databases
{
    abstract partial class DatabasePool : StrictlyManagedResourceBase<DatabasePool>
    {
        protected readonly MachineWideSharedObject<SharedState> MachineWideState;
        protected static string? DatabaseRootFolderOverride;
        static TimeSpan _reservationLength;
        protected const int NumberOfDatabases = 10;

        protected DatabasePool()
        {
            _reservationLength = System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 65.Seconds();

            if(ComposableTempFolder.IsOverridden)
            {
                DatabaseRootFolderOverride = ComposableTempFolder.EnsureFolderExists("DatabasePoolData");
            }

            MachineWideState = MachineWideSharedObject<SharedState>.For(GetType().GetFullNameCompilable().ReplaceInvariant(".", "_"), usePersistentFile: true);
        }

        internal static readonly string PoolDatabaseNamePrefix = $"Composable_{nameof(DatabasePool)}_";

        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(30.Seconds());
        readonly Guid _poolId = Guid.NewGuid();
        IReadOnlyList<Database> _transientCache = new List<Database>();

        static ILogger Log = Logger.For<DatabasePool>();
        bool _disposed;
        const string RebootedDatabaseExceptionMessage = "Something went wrong with the database pool and it was rebooted. You may see other test failures due to this. If this is the first time you use the pool everything is fine. If this error pops up at other times something is amiss.";

        public void SetLogLevel(LogLevel logLevel) => _guard.Update(() => Log = Log.WithLogLevel(logLevel));

        public string ConnectionStringFor(string reservationName) => _guard.Update(() =>
        {
            // ReSharper disable once InconsistentlySynchronizedField
            Contract.Assert.That(!_disposed, "!_disposed");

            var reservedDatabase = _transientCache.SingleOrDefault(db => db.ReservationName == reservationName);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(reservedDatabase != null)
            {
                Log.Debug($"Retrieved reserved pool database: {reservedDatabase.Id}");
                return ConnectionStringFor(reservedDatabase);
            }

            var startTime = DateTime.Now;
            var timeoutAt = startTime + 45.Seconds();
            while(reservedDatabase == null)
            {
                if(DateTime.Now > timeoutAt)
                {
                    throw new Exception("Timed out waiting for database. Have you missed disposing a database pool? Please check your logs for errors about non-disposed pools.");
                }

                MachineWideState.Update(
                    machineWide =>
                    {
                        if(machineWide.IsEmpty) throw new Exception("MachineWide was empty.");
                        if(!machineWide.IsValid) throw new Exception("Detected corrupt database pool.");

                        if(machineWide.TryReserve(reservationName, _poolId, _reservationLength, out reservedDatabase))
                        {
                            Log.Info($"Reserved pool database: {reservedDatabase.Name}");
                            _transientCache = machineWide.DatabasesReservedBy(_poolId);
                            //Todo:My tests so far show no performance increase from enabling this. Maybe if we used async all the way so hardly any threads were used? If we don't see an meaningful improvement. Remove this method.
                            //CleanDataBasesInBackgroundTasks(machineWide);
                        }
                    });

                if(reservedDatabase == null)
                {
                    Thread.Sleep(10);
                }
            }

            if(!reservedDatabase!.IsClean)
            {
                try
                {
                    TransactionScopeCe.SuppressAmbient(() => ResetDatabase(reservedDatabase));
                }
                catch(Exception exception)
                {
                    Log.Error(exception);
                    EnsureDatabaseExistsAndIsEmpty(reservedDatabase);
                }
            }

            return ConnectionStringFor(reservedDatabase);
        });

        static readonly string BackgroundResetDatabaseTaskName = $"{nameof(DatabasePool)}_{nameof(CleanDataBasesInBackgroundTasks)}";
        void CleanDataBasesInBackgroundTasks(SharedState machineWide)
        {
            machineWide.ReserveDatabasesForCleaning(_poolId)
                       .ForEach(reserved =>
                        {
                            TaskCE.Run(BackgroundResetDatabaseTaskName,
                                       () =>
                                       {
                                           //We are being a bit tricky here. The databases are reserved by this instance so that when it is disposed so are their reservations.
                                           //That way we don't leave our reservations around when we don't have time to clean them before the test runner terminates.
                                           //But that means we must be careful not to mess with databases that have been released when this pool was disposed.
                                           lock(_disposeLock)
                                           {
                                               if(!_disposed)
                                               {
                                                   TransactionScopeCe.SuppressAmbient(() => ResetDatabase(reserved));
                                                   MachineWideState.Update(innerMachineWide => innerMachineWide.ReleaseClean(reserved.ReservationName));
                                               }
                                           }
                                       });
                        });
        }

        protected abstract void ResetDatabase(Database db);

        protected abstract string ConnectionStringFor(Database db);

        readonly object _disposeLock = new object();
        protected override void Dispose(bool disposing)
        {
            lock(_disposeLock)
            {
                if(_disposed) return;
                _disposed = true;
            }

            MachineWideState.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
            MachineWideState.Dispose();
            base.Dispose(disposing);
        }

        protected abstract void EnsureDatabaseExistsAndIsEmpty(Database db);
    }
}
