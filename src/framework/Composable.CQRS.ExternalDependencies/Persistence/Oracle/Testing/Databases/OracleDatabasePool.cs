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
        static bool DoneWithHack = false;

        void UglyHackOpenAllConnectionsThreadedToSpeedUpSubsequentOpenings()
        {
            lock(Lock)
            {
                if(!DoneWithHack)
                {
                    var connectionTasks = _machineWideState
                                         .GetCopy()
                                         .Databases
                                         .Select(ConnectionStringFor)
                                         .Append(_masterConnectionString)
                                         .Select(connectionString => new Action(() => new OracleConnectionProvider(connectionString).UseConnection(_ => {})))
                                         .ToArray();

                    Console.WriteLine(StopwatchExtensions.TimeExecutionThreaded(connectionTasks, swallowExceptions: true, description: "Open connections with all oracle users."));
                    DoneWithHack = true;
                }
            }
        }

        protected override string ConnectionStringFor(Database db)
            => new OracleConnectionStringBuilder(_masterConnectionString) {UserID = db.Name.ToUpper(), Password = db.Name.ToUpper(), DBAPrivilege = ""}.ConnectionString;

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {








            //ResetConnectionPool(db);
            ResetDatabase(db);
        }

        protected override void ResetDatabase(Database db)
        {
            var dropUserIfExistsAndRecreate = DropUserIfExistsAndRecreate(db.Name.ToUpper());
            //Console.WriteLine(dropUserIfExistsAndRecreate);
            //ResetConnectionPool(db);
            _masterConnectionProvider.UseConnection(_ => {});
            //_masterConnectionProvider.ExecuteNonQuery(CreateUserIfNotExists(db.Name.ToUpper()));
            var localConnection = new OracleConnectionProvider(ConnectionStringFor(db));
            localConnection.UseConnection(_ => {});
            localConnection.ExecuteNonQuery(CleanSchema());
        }

        static string CreateUserIfNotExists(string oracleUserName) => $@"
declare user_to_drop_exists integer;
begin
    select count(*) into user_to_drop_exists from dba_users where username='{oracleUserName}';
    if (user_to_drop_exists = 0) then
        --execute immediate 'DROP USER ""{oracleUserName}"" CASCADE';
        execute immediate 'CREATE USER ""{oracleUserName}"" IDENTIFIED BY ""{oracleUserName}""';
        -- ROLES
        execute immediate 'GRANT DBA TO ""{oracleUserName}"" WITH ADMIN OPTION';
        -- SYSTEM PRIVILEGES
        execute immediate 'GRANT SYSDBA TO ""{oracleUserName}""';
    end if;
end;
";

        static string DropUserIfExistsAndRecreate(string oracleUserName) => $@"
declare user_to_drop_exists integer;
begin
 {DropUser(oracleUserName)}
 {CreateUser(oracleUserName)}
end;
";
        static string DropUser(string oracleUserName) => $@"
    select count(*) into user_to_drop_exists from dba_users where username='{oracleUserName}';
    if (user_to_drop_exists > 0) then
        DECLARE
          v_user_exists NUMBER;
          user_name CONSTANT varchar2(50) := '{oracleUserName}';
        BEGIN
          LOOP
            BEGIN
              EXECUTE IMMEDIATE 'drop user ' || user_name || ' cascade';
              EXCEPTION WHEN OTHERS THEN
              IF (SQLCODE = -1940) THEN
                NULL;
              ELSE
                RAISE;
              END IF;
            END;

            BEGIN
              SELECT COUNT(*) INTO v_user_exists FROM dba_users WHERE username = user_name;
              EXIT WHEN v_user_exists = 0;
            END;

            FOR c IN (SELECT s.sid, s.serial# FROM v$session s WHERE upper(s.username) = user_name)
            LOOP
              EXECUTE IMMEDIATE
                'alter system kill session ''' || c.sid || ',' || c.serial# || ''' IMMEDIATE';
            END LOOP;            
          END LOOP;
        END;
        --execute immediate 'DROP USER ""{oracleUserName}"" CASCADE';
    end if;
";

        static string CreateUser(string oracleUserName) => $@"    
    execute immediate 'CREATE USER ""{oracleUserName}"" IDENTIFIED BY ""{oracleUserName}""';
    -- ROLES
    execute immediate 'GRANT DBA TO ""{oracleUserName}"" WITH ADMIN OPTION';
    -- SYSTEM PRIVILEGES
    execute immediate 'GRANT SYSDBA TO ""{oracleUserName}""';
";

        static string CleanSchema() => $@"
BEGIN
    FOR cur_rec IN (SELECT object_name, object_type
                  FROM   user_objects
                  WHERE  object_type IN ('TABLE')) LOOP
    BEGIN
        IF cur_rec.object_type = 'TABLE' THEN
            EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' ""' || cur_rec.object_name || '"" CASCADE CONSTRAINTS';
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
