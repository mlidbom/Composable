using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.Testing.Databases;

namespace Composable.Persistence.Oracle.Testing.Databases
{
    sealed class OracleDatabasePool : DatabasePool
    {
        readonly string _masterConnectionString;
        readonly OracleConnectionProvider _masterConnectionProvider;



        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        public OracleDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName);

            _masterConnectionString = masterConnectionString ?? "Data Source=localhost:1521/orclpdb; DBA Privilege=SYSDBA; User Id=sys; Password=Development!1;";

            _masterConnectionString = _masterConnectionString.Replace("\\", "_");

            _masterConnectionProvider = new OracleConnectionProvider(_masterConnectionString);

            UglyHackOpenAllConnectionsThreadedToSpeedUpSubsequentOpenings();
        }

        static object Lock = new object();
        static bool DoneWithHack;

        ///<summary>Current oracle ADO implementation takes several seconds to open the first connection per unique connection string. This code divides the slowdown by the number of databases in the pool very significantly speeding up tests.</summary>
        void UglyHackOpenAllConnectionsThreadedToSpeedUpSubsequentOpenings()
        {
            lock(Lock)
            {
                if(!DoneWithHack)
                {
                    try
                    {
                        Task.WaitAll(_machineWideState
                                    .GetCopy()
                                    .Databases
                                    .Select(ConnectionStringFor)
                                    .Append(_masterConnectionString)
                                    .Select(connectionString => Task.Factory.StartNew(() => new OracleConnectionProvider(connectionString).UseConnection(_ => {}), TaskCreationOptions.LongRunning))
                                    .ToArray());
                    }
                    catch(Exception) {}
                    finally
                    {
                        DoneWithHack = true;
                    }
                }
            }
        }

        protected override string ConnectionStringFor(Database db)
            => new OracleConnectionStringBuilder(_masterConnectionString) {UserID = db.Name.ToUpper(), Password = db.Name.ToUpper(), DBAPrivilege = ""}.ConnectionString;

        const int OracleInvalidUserNamePasswordCombinationErrorNumber = 1017;
        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            try
            {
                ResetDatabase(db);
            }
            catch(OracleException exception) when(exception.Number == OracleInvalidUserNamePasswordCombinationErrorNumber)
            {
                _masterConnectionProvider.ExecuteScalar(DropUserIfExistsAndRecreate(db.Name.ToUpper()));
                new OracleConnectionProvider(ConnectionStringFor(db)).UseConnection(_ => {});//We just call this to ensure that we can actually connect.
            }
        }

        protected override void ResetDatabase(Database db) => new OracleConnectionProvider(ConnectionStringFor(db)).ExecuteNonQuery(CleanSchema());

        static string DropUserIfExistsAndRecreate(string userName) => $@"
declare user_to_drop_exists integer;
begin
    select count(*) into user_to_drop_exists from dba_users where username='{userName}';
    if (user_to_drop_exists > 0) then
        EXECUTE IMMEDIATE 'drop user ""{userName}"" cascade';        
    end if;
    
    execute immediate 'CREATE USER ""{userName}"" IDENTIFIED BY ""{userName}""';
    -- ROLES
    execute immediate 'GRANT DBA TO ""{userName}"" WITH ADMIN OPTION';
    -- SYSTEM PRIVILEGES
    execute immediate 'GRANT SYSDBA TO ""{userName}""';
end;
";

        static string CleanSchema() => @"
BEGIN
    FOR cur_rec IN (SELECT object_name, object_type FROM   user_objects) LOOP
        BEGIN
            IF cur_rec.object_type = 'TABLE' THEN
                EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' ""' || cur_rec.object_name || '"" CASCADE CONSTRAINTS';
            ELSIF cur_rec.object_type = 'TYPE' THEN
                EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' ""' || cur_rec.object_name || '"" FORCE';
            ELSE
                EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' ""' || cur_rec.object_name || '""';
            END IF;
            END;
    END LOOP;
END;
";

        void ResetConnectionPool(Database db)
        {
            using var connection = new OracleConnection(this.ConnectionStringFor(db));
            OracleConnection.ClearPool(connection);
        }
    }
}
