using System;
using Composable.Persistence.Common.AdoCE;
using Oracle.ManagedDataAccess.Client;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Databases;

namespace Composable.Persistence.Oracle.Testing.Databases
{
    sealed class OracleDatabasePool : DatabasePool
    {
        readonly IOracleConnectionPool _masterConnectionPool;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_ORACLE_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        readonly OptimizedThreadShared<OracleConnectionStringBuilder> _connectionStringBuilder;

        public OracleDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                      ?? "Data Source=127.0.0.1:1521/orclpdb; DBA Privilege=SYSDBA; User Id=sys; Password=Development!1;";

            _connectionStringBuilder = new OptimizedThreadShared<OracleConnectionStringBuilder>(new OracleConnectionStringBuilder(masterConnectionString));
            _masterConnectionPool = IOracleConnectionPool.CreateInstance(masterConnectionString);
        }

        protected override string ConnectionStringFor(Database db)
            => _connectionStringBuilder.WithExclusiveAccess(@this => @this.Mutate(me =>
            {
                me.UserID = db.Name.ToUpperInvariant();
                me.Password = db.Name.ToUpperInvariant();
                me.DBAPrivilege = "";
            }).ConnectionString);

        const int OracleInvalidUserNamePasswordCombinationErrorNumber = 1017;
        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            try
            {
                ResetDatabase(db);
            }
            catch(OracleException exception) when(exception.Number == OracleInvalidUserNamePasswordCombinationErrorNumber)
            {
                _masterConnectionPool.ExecuteScalar(DropUserIfExistsAndRecreate(db.Name.ToUpperInvariant()));
                IOracleConnectionPool.CreateInstance(ConnectionStringFor(db)).UseConnection(_ => {}); //We just call this to ensure that we can actually connect.
            }
        }

        protected override void ResetDatabase(Database db)
        {
            IOracleConnectionPool.CreateInstance(ConnectionStringFor(db))
                                     .UseCommand(command => command.SetCommandText(CleanSchema()).ExecuteNonQuery());
        }

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
    FOR cur_rec IN (SELECT object_name, object_type FROM user_objects 
                        WHERE object_type not in ('INDEX','PACKAGE BODY','TRIGGER','LOB', 'SEQUENCE')) LOOP
        BEGIN
            IF cur_rec.object_type = 'TABLE' THEN
                EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' ""' || cur_rec.object_name || '"" CASCADE CONSTRAINTS PURGE';
            ELSIF cur_rec.object_type = 'TYPE' THEN
                EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' ""' || cur_rec.object_name || '"" FORCE';
            ELSE
                EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' ""' || cur_rec.object_name || '""';
            END IF;
        END;
    END LOOP;
END;
";
    }
}
