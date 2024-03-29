using System;
using IBM.Data.DB2.Core;
using System.Linq;
using System.Transactions;
using Composable.Logging;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.SystemCE;
using Composable.Testing.Databases;

namespace Composable.Persistence.DB2.Testing.Databases
{
    sealed class DB2DatabasePool : DatabasePool
    {
        readonly IDB2ConnectionPool _masterConnectionPool;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_DB2_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        readonly string _masterConnectionString;

        public DB2DatabasePool()
        {
            _masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                   ?? "SERVER=localhost;DATABASE=CDBPOOL;User ID=db2admin;Password=Development!1;";

            _masterConnectionPool = IDB2ConnectionPool.CreateInstance(_masterConnectionString);
        }

        protected override string ConnectionStringFor(Database db)
            => _masterConnectionString + $"CurrentSchema={db.Name.ToUpperInvariant()};";

        const string ObjectAlreadyExists = "42710";
        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            try
            {
                _masterConnectionPool.ExecuteNonQuery($@"CREATE SCHEMA ""{db.Name.ToUpperInvariant()}""");
            }
            catch(DB2Exception exception) when(exception.Errors.Cast<DB2Error>().Any(error => error.SQLState == ObjectAlreadyExists)) {}

            ResetDatabase(db);
        }

        protected override void ResetDatabase(Database db)
        {
            if(Transaction.Current != null) throw new Exception("This code should never run in a transaction");

            //Splitting this into one call to get the drop statements and another to execute them seems to perform about three times faster than doing everything on the server as an SP. It also eliminated the deadlocks we were getting.
            var dropStatements = _masterConnectionPool.UseCommand(
                command => command.SetCommandText(GetRemovalStatementsSql)
                                  .AddParameter(SchemaParameterName, DB2Type.VarChar, db.Name.ToUpperInvariant())
                                  .ExecuteReaderAndSelect(reader =>
                                                              new
                                                              {
                                                                  CreateTime = reader.GetDateTime(0),
                                                                  DropStatement = reader.GetString(1)
                                                              })
                                  .OrderByDescending(me => me.CreateTime)
                                  .Select(me => me.DropStatement)
                                  .Where(me => !me.IsNullEmptyOrWhiteSpace())
                                  .Join($";{Environment.NewLine}")).Trim();

            if(dropStatements.Length > 0)
            {
                dropStatements += ";";

                for(int retries = 0; retries < 3; retries++)
                {
                    try
                    {
                        _masterConnectionPool.ExecuteNonQuery(dropStatements);
                        return;
                    }
                    catch(DB2Exception exception)when(SqlExceptions.DB2.IsDeadlockOrTimeOut(exception))
                    {
                        this.Log().Error(exception);
                    }
                }

                throw new Exception("Failed to clean DB2 database");
            }
        }

        const string SchemaParameterName = "Schema";
        const string GetRemovalStatementsSql = $@"

SELECT CREATE_TIME, 
    'DROP ' || CASE TYPE
        WHEN 'A' THEN 'ALIAS'
        WHEN 'H' THEN 'TABLE'
        WHEN 'N' THEN 'NICKNAME'
        WHEN 'S' THEN 'TABLE'
        WHEN 'T' THEN 'TABLE'
        WHEN 'U' THEN 'TABLE'
        WHEN 'V' THEN 'VIEW'
        WHEN 'W' THEN 'VIEW'
    END || ' ' || TRIM(TABSCHEMA) || '.' || TRIM(TABNAME) AS DDL
FROM SYSCAT.TABLES WHERE TABSCHEMA = @{SchemaParameterName}
FOR READ ONLY WITH UR
";

        /*--Currently we don't use these object types and adding them above makes ResetDB significantly slower.
                static readonly string GetUnusedRemovalStatements = $@"
        SELECT CREATE_TIME, 
            'DROP ' || CASE TYPE
                WHEN 'A' THEN 'ALIAS'
                WHEN 'H' THEN 'TABLE'
                WHEN 'N' THEN 'NICKNAME'
                WHEN 'S' THEN 'TABLE'
                WHEN 'T' THEN 'TABLE'
                WHEN 'U' THEN 'TABLE'
                WHEN 'V' THEN 'VIEW'
                WHEN 'W' THEN 'VIEW'
            END || ' ' || TRIM(TABSCHEMA) || '.' || TRIM(TABNAME) AS DDL
        FROM SYSCAT.TABLES WHERE TABSCHEMA = @{SchemaParameterName}
            UNION
            SELECT CREATE_TIME,
                'DROP TRIGGER ' || TRIM(TRIGSCHEMA) || '.' || TRIM(TRIGNAME) AS DDL
            FROM SYSCAT.TRIGGERS WHERE TRIGSCHEMA = @
        {SchemaParameterName}
            UNION
            SELECT CREATE_TIME,
                'DROP ' || CASE ROUTINETYPE
                    WHEN 'F' THEN 'SPECIFIC FUNCTION'
                    WHEN 'M' THEN 'SPECIFIC METHOD'
                    WHEN 'P' THEN 'SPECIFIC PROCEDURE'
                END || ' ' || TRIM(ROUTINESCHEMA) || '.' || TRIM(SPECIFICNAME) AS DDL
            FROM SYSCAT.ROUTINES WHERE ROUTINESCHEMA = @{SchemaParameterName}
            UNION
            SELECT CREATE_TIME,
                'DROP TYPE ' || TRIM(TYPESCHEMA) || '.' || TRIM(TYPENAME) AS DDL
            FROM SYSCAT.DATATYPES WHERE TYPESCHEMA = @{SchemaParameterName}
            UNION
            SELECT CREATE_TIME, 
                'DROP SEQUENCE ' || TRIM(SEQSCHEMA) || '.' || TRIM(SEQNAME) AS DDL
            FROM SYSCAT.SEQUENCES WHERE SEQTYPE <> 'I' AND SEQSCHEMA = @{SchemaParameterName}";
        
        */
    }
}
