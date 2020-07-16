using System;
using System.Collections.Generic;
using System.Data;
using IBM.Data.DB2.Core;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using Composable.Testing.Databases;

namespace Composable.Persistence.DB2.Testing.Databases
{
    sealed class DB2DatabasePool : DatabasePool
    {
        readonly DB2ConnectionProvider _masterConnectionProvider;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_DB2_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        readonly string _masterConnectionString;

        public DB2DatabasePool()
        {
            _masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                   ?? "SERVER=localhost;DATABASE=CDBPOOL;User ID=db2admin;Password=Development!1;";

            _masterConnectionProvider = new DB2ConnectionProvider(_masterConnectionString);
        }

        protected override string ConnectionStringFor(Database db)
            => _masterConnectionString + $"CurrentSchema={db.Name};";

        protected override void InitReboot() => SystemProcedures.CreateProcedures(_masterConnectionProvider);

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db) => ResetDatabase(db);

        protected override void ResetDatabase(Database db)
        {
             _masterConnectionProvider.ExecuteNonQuery($@"
CALL DROP_SCHEMA('{db.Name}');
CREATE SCHEMA ""{db.Name}""");
        }
    }
}