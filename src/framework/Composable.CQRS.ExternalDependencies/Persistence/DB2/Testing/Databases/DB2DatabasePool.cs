using System;
using System.Collections.Generic;
using System.Data;
using IBM.Data.DB2.Core;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Logging;
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
            => _masterConnectionString + $"CurrentSchema={db.Name.ToUpperInvariant()};";

        protected override void InitReboot() => SystemProcedures.CreateProcedures(_masterConnectionProvider);


        const string ObjectAlreadyExists = "42710";
        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            try
            {
                _masterConnectionProvider.ExecuteNonQuery($@"CREATE SCHEMA ""{db.Name.ToUpperInvariant()}""");
            }
            catch(DB2Exception exception) when(exception.Errors.Cast<DB2Error>().Any(error => error.SQLState == ObjectAlreadyExists))
            {}

            ResetDatabase(db);
        }

        protected override void ResetDatabase(Database db) =>
            _masterConnectionProvider.UseCommand(command => command.SetStoredProcedure("EMPTY_SCHEMA")
                                                                   .AddParameter("ASCHEMA", DB2Type.VarChar, db.Name.ToUpperInvariant())
                                                                   .ExecuteNonQuery());
    }
}