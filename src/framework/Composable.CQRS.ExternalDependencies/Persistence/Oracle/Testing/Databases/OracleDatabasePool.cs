using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System;
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



        }

        protected override string ConnectionStringFor(Database db)
            => new OracleConnectionStringBuilder(_masterConnectionString) {UserID = db.Name.ToUpper(), Password = db.Name.ToUpper()}.ConnectionString;

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {








            ResetConnectionPool(db);
            var oracleUserName = db.Name.ToUpper();
            _masterConnectionProvider.ExecuteNonQuery($@"
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
");
            ResetDatabase(db);
        }

        protected override void ResetDatabase(Database db)
        {
            new OracleConnectionProvider(ConnectionStringFor(db)).ExecuteNonQuery(CleanSchema(db.Name.ToUpper()));
        }

        static string CleanSchema(string databaseName) => $@"
BEGIN
    FOR cur_rec IN (SELECT object_name, object_type
                  FROM   all_objects
                  WHERE  object_type IN ('TABLE', 'VIEW', 'PACKAGE', 'PROCEDURE', 'FUNCTION', 'SEQUENCE','SYNONYM','TYPE','TABLE PARTITION') AND 
                  owner = '{databaseName}') LOOP
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
