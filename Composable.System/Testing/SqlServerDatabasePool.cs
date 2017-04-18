using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.System;
using Composable.System.Configuration;
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

        public ISqlConnectionProvider ConnectionProviderFor(string connectionStringName)
        {
            Contract.Assert.That(!_disposed, "!_disposed");

            Database database;
            if(_reservedDatabases.TryGetValue(connectionStringName, out database))
                return new ConnectionProvider(database, this);

            _machineWideState.Update(machineWide =>
                                    {
                                        if (machineWide.Databases == null)
                                        {
                                            machineWide.Databases = TransactionScopeCe.SupressAmbient<IReadOnlyList<Database>>(ListPoolDatabases).ToList();
                                        }

                                        TransactionScopeCe.SupressAmbient(action: () =>
                                                                         {
                                                                             if(TryReserveDatabase(machineWide, out database))
                                                                             {
                                                                                 _reservedDatabases.Add(connectionStringName, database);
                                                                             } else
                                                                             {
                                                                                 ReleaseOldLocks(machineWide);
                                                                                 if(TryReserveDatabase(machineWide, out database))
                                                                                 {
                                                                                     _reservedDatabases.Add(connectionStringName, database);
                                                                                 } else
                                                                                 {
                                                                                     database = InsertDatabase(machineWide);
                                                                                     _reservedDatabases.Add(connectionStringName, database);
                                                                                 }
                                                                             }
                                                                         });
                                    });

            return new ConnectionProvider(database, this);
        }

        internal string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString.Replace(_initialCatalogMaster,$";Initial Catalog={dbName};");

        static bool TryReserveDatabase(SharedState machineWide, out Database reserved) => machineWide.TryReserve(out reserved);

        Database InsertDatabase(SharedState machineWide)
        {
            Database database = machineWide.Insert();

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
                new SqlServerConnectionProvider(ConnectionStringForDbNamed(database.Name())).UseConnection(action: connection => connection.DropAllObjects());

                machineWide.Release(database.Name());
            }

            databases.ForEach(action: db => _reservedDatabases.Remove(db.Name()));
            databases.ForEach(CleanAndReleaseDatabase);
        }

        void CleanAndRelease(IReadOnlyList<Database> databases)
        {
            void CleanAndReleaseDatabase(Database database)
            {
                 new SqlServerConnectionProvider(ConnectionStringForDbNamed(database.Name())).UseConnection(action: connection => connection.DropAllObjects());
                _machineWideState.Update(machineWide => machineWide.Release(database.Name()));
            }

            databases.ForEach(action: db => _reservedDatabases.Remove(db.Name()));
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
