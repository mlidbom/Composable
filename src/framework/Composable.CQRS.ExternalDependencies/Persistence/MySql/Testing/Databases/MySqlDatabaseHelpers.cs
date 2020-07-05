using System.Data;
using Composable.Persistence.MySql.SystemExtensions;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.Testing.Databases
{
    static class MySqlDatabaseHelpers
    {
        //Urgent: Check if something like this is beneficial for MySql. I created it for Sql Server because creating a database is too expensive. Is it in MySql?
//        static readonly string DropAllObjectsStatement = @"

//DECLARE @statements nvarchar(max)

//select @statements = COALESCE(@statements + '
//', '') + statement
//FROM (SELECT CASE
//        WHEN type = 'AF'                           THEN 'DROP AGGREGATE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//        WHEN type = 'R'                            THEN 'DROP RULE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//        WHEN type = 'SN'                           THEN 'DROP SYNONYM ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//        WHEN type = 'U'                            THEN 'DROP TABLE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//        WHEN type = 'V'                            THEN 'DROP VIEW ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//        WHEN type IN('C', 'F', 'UQ')               THEN 'ALTER TABLE ' + QUOTENAME(object_schema_name(parent_object_id)) + '.' + QUOTENAME(object_name(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name)
//        WHEN type IN('FN', 'FS', 'FT', 'IF', 'TF') THEN 'DROP FUNCTION ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//        WHEN type IN('P', 'PC')                    THEN 'DROP PROCEDURE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//        WHEN type IN('TA', 'TR')                   THEN 'DROP TRIGGER ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
//    END statement,
//    type FROM sys.objects) as dropStatements
//    WHERE statement IS NOT NULL
//    ORDER BY CASE
//          WHEN type IN('F') THEN 1
//          WHEN type IN('C', 'TA', 'TR') THEN 2
//          WHEN type IN('UQ') THEN 3
//          ELSE 4
//      END

//execute sp_executesql @statements
//";

static string DropAllObjectsStatement(string dbName) => $@"
DROP DATABASE {dbName};
CREATE DATABASE {dbName};
USE {dbName};";

//Urgent: check for equivalent in MySql
//        internal static readonly string SetReadCommittedSnapshotOnStatement = @"
//declare @databaseName varchar(1000)
//select @databaseName = DB_NAME()
//declare @sql nvarchar(500)
//set @sql = 'ALTER DATABASE [' + @databaseName +  '] SET READ_COMMITTED_SNAPSHOT ON'
//exec sp_executesql @sql";

        internal static void DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel(this MySqlConnection connection, string name) => connection.ExecuteNonQuery(DropAllObjectsStatement(name));
    }
}
