using System;
using System.Collections.Generic;
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

        readonly OptimizedThreadShared<DB2ConnectionStringBuilder> _connectionStringBuilder;

        public DB2DatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                      ?? "SERVER=localhost;DATABASE=CDBPOOL;CurrentSchema={schema};User ID=db2admin;Password=Development!1;";

            _connectionStringBuilder = new OptimizedThreadShared<DB2ConnectionStringBuilder>(new DB2ConnectionStringBuilder(masterConnectionString));
            _masterConnectionProvider = new DB2ConnectionProvider(masterConnectionString);
        }

        protected override string ConnectionStringFor(Database db)
            => _connectionStringBuilder.WithExclusiveAccess(@this => @this.Mutate(me => me.CurrentSchema = db.Name).ConnectionString);

        const int DB2InvalidUserNamePasswordCombinationErrorNumber = 1017;
        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            try
            {
                ResetDatabase(db);
            }
            //urgent: This is a number from oracle. Find a DB2 way
            catch(DB2Exception exception) when(exception.ErrorCode == DB2InvalidUserNamePasswordCombinationErrorNumber)
            {
                _masterConnectionProvider.ExecuteScalar(DropUserIfExistsAndRecreate(db.Name.ToUpper()));
                new DB2ConnectionProvider(ConnectionStringFor(db)).UseConnection(_ => {}); //We just call this to ensure that we can actually connect.
            }
        }

        protected override void ResetDatabase(Database db) { new DB2ConnectionProvider(ConnectionStringFor(db)).ExecuteNonQuery(CleanSchema()); }

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
