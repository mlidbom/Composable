using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence;
using Composable.SystemCE;
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
        const int NumberOfDatabases = 10;

        protected DatabasePool()
        {
            _reservationLength = System.Diagnostics.Debugger.IsAttached ? 10.Minutes() : 65.Seconds();

            if(ComposableTempFolder.IsOverridden)
            {
                DatabaseRootFolderOverride = ComposableTempFolder.EnsureFolderExists("DatabasePoolData");
            }

            MachineWideState = MachineWideSharedObject<SharedState>.For(GetType().GetFullNameCompilable().ReplaceInvariant(".", "_"), usePersistentFile: true);
        }

        const string PoolDatabaseNamePrefix = $"Composable_{nameof(DatabasePool)}_";

        readonly MonitorCE _guard = MonitorCE.WithTimeout(30.Seconds());
        readonly Guid _poolId = Guid.NewGuid();
        IReadOnlyList<Database> _transientCache = new List<Database>();

        static ILogger Log = Logger.For<DatabasePool>();
        bool _disposed;

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
                if(DateTime.Now > timeoutAt) throw new Exception("Timed out waiting for database. Have you missed disposing a database pool? Please check your logs for errors about non-disposed pools.");

                MachineWideState.Update(
                    machineWide =>
                    {
                        if(machineWide.TryReserve(reservationName, _poolId, _reservationLength, out reservedDatabase))
                        {
                            Log.Info($"Reserved pool database: {reservedDatabase.Name}");
                            _transientCache = machineWide.DatabasesReservedBy(_poolId);
                        }
                    });

                if(reservedDatabase == null)
                {
                    Thread.Sleep(10);
                }
            }

            try
            {
                TransactionScopeCe.SuppressAmbient(() => ResetDatabase(reservedDatabase));
            }
            catch(Exception exception)
            {
                Log.Error(exception);
                EnsureDatabaseExistsAndIsEmpty(reservedDatabase);
            }

            return ConnectionStringFor(reservedDatabase);
        });

        protected abstract void ResetDatabase(Database db);

        protected abstract string ConnectionStringFor(Database db);

        protected override void Dispose(bool disposing)
        {
            if(_disposed) return;
            _disposed = true;


            MachineWideState.Update(machineWide => machineWide.ReleaseReservationsFor(_poolId));
            MachineWideState.Dispose();
            base.Dispose(disposing);
        }

        protected abstract void EnsureDatabaseExistsAndIsEmpty(Database db);
    }
}
